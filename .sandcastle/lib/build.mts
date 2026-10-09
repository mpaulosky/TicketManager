// Build one planned issue in a single sandbox: implement, check, review, merge
// main in, check whatever changed, and publish the checked commit as the
// issue's own pull request. Nothing is merged into a local branch and no issue
// is closed here: the PR says "Fixes #n", so the issue closes when the PR
// merges through the normal checks and review in docs/PROCESS.md.

import * as sandcastle from "@ai-hero/sandcastle";
import { docker } from "@ai-hero/sandcastle/sandboxes/docker";
import { commitsAhead, fetchMain } from "./branches.mts";
import { fenced, headOf, isDirty, runCheck, tail } from "./check.mts";
import { CHECK_COMMENT_LINES, copyToWorktree, hooks, IMPLEMENTER_ITERATIONS, MODEL } from "./config.mts";
import { commentOnIssue, openPullRequest, type SandcastleIssue } from "./github.mts";
import { issuePromptArgs } from "./prompts.mts";
import { prBody, prTitle } from "./publish.mts";
import { git, gitConfigIntact } from "./shell.mts";
import { parseVerdict, type Verdict } from "./verdict.mts";

// The parts of a sandbox buildIssue uses; tests pass a fake.
export type BuildSandbox = Pick<sandcastle.Sandbox, "run" | "exec" | "close" | "worktreePath">;

// What buildIssue needs from outside the pipeline; tests pass stubs.
export type BuildHost = {
	// A sandbox on the branch, which starts from `mainSha` when it's new.
	createSandbox(branch: string, mainSha: string): Promise<BuildSandbox>;
	// Fetch main and return its commit id, as origin reports it.
	fetchMain(): string;
	commitsAhead(mainSha: string, sha: string): number;
	commentOnIssue(issueNumber: number, body: string): void;
	// Push the commit to the branch on origin and open (or reuse) its pull request.
	publish(branch: string, sha: string, title: string, body: string): string;
	// False once an agent may have rewritten the clone's git config or
	// redirected the worktree's git files (see GitConfigGuard and worktreeIntact).
	gitConfigIntact(worktreePath: string): boolean;
	log(line: string): void;
};

// Push the exact commit the check passed on, from the host's own checkout
// with hooks off (see git in shell.mts): the pre-push hook in the worktree is
// a file the agents could have edited. CI runs the full suite on the PR. No
// force: a branch whose history was rewritten fails here and is reported.
function publish(branch: string, sha: string, title: string, body: string): string {
	git(process.cwd(), "push", "origin", `${sha}:refs/heads/${branch}`);
	return openPullRequest(branch, title, body);
}

const liveHost: BuildHost = {
	createSandbox: (branch, mainSha) =>
		sandcastle.createSandbox({ branch, baseBranch: mainSha, sandbox: docker(), hooks, copyToWorktree }),
	fetchMain,
	commitsAhead,
	commentOnIssue,
	publish,
	gitConfigIntact,
	log: console.log,
};

export type BuildOutcome =
	| "published"
	| "nothing-to-publish"
	| "implementer-unfinished"
	| "check-failed"
	| "rejected"
	| "conflicts-with-main"
	| "publish-failed";

// `mainSha` is main's commit as the host fetched it for this round.
export async function buildIssue(
	issue: SandcastleIssue,
	branch: string,
	mainSha: string,
	host: BuildHost = liveHost,
): Promise<{ outcome: BuildOutcome; prUrl?: string }> {
	const log = (line: string) => host.log(`  #${issue.number} ${line}`);
	const stop = (outcome: BuildOutcome, comment: string) => {
		log(`stopped: ${outcome}`);
		host.commentOnIssue(issue.number, comment);
		return { outcome };
	};
	const notPushed = `\`${branch}\` wasn't pushed; the local branch keeps its commits.`;
	const promptArgs = issuePromptArgs(issue, branch, mainSha);
	let main = mainSha;

	const sandbox = await host.createSandbox(branch, mainSha);
	try {
		// Implement. A run that throws or uses up its iterations without
		// signalling completion stops the issue for this round.
		let finished: boolean;
		let failure = "ran out of iterations unfinished";
		try {
			const implement = await sandbox.run({
				name: "implementer",
				agent: sandcastle.claudeCode(MODEL),
				maxIterations: IMPLEMENTER_ITERATIONS,
				promptFile: "./.sandcastle/implement-prompt.md",
				promptArgs,
			});
			finished = implement.completionSignal !== undefined;
		} catch (error) {
			finished = false;
			failure = `failed: ${error}`;
		}
		if (!finished) {
			return stop("implementer-unfinished", `Sandcastle stopped building this issue: the implementer ${failure}. ${notPushed}`);
		}

		// Run the check; on a pass, return the commit it passed on, the only
		// commit that may be pushed. On a failure, say so on the issue.
		const check = async (when: string): Promise<string | undefined> => {
			const result = await runCheck(sandbox, main);
			log(`check ${when}: ${result.passed ? "passed" : "failed"}`);
			if (result.passed) return result.head;
			host.commentOnIssue(
				issue.number,
				`Sandcastle stopped building this issue: \`.sandcastle/check.sh\` failed ${when}. ${notPushed}\n\n` +
					`The last ${CHECK_COMMENT_LINES} lines of its output:\n\n${fenced(tail(result.output, CHECK_COMMENT_LINES))}`,
			);
			return undefined;
		};

		// Check again whenever HEAD isn't the commit last checked: a commit, an
		// amend, a reset or a merge all count, not only new commits.
		let checked = await check("after the implementer");
		if (!checked) return { outcome: "check-failed" };

		// Gate, review and publish whenever the branch holds work main doesn't,
		// not only when this run added commits: a re-run of a finished issue
		// makes none, and its earlier work still needs a PR.
		if (host.commitsAhead(main, checked) === 0) {
			return stop(
				"nothing-to-publish",
				"Sandcastle's implementer finished this issue without changing anything main doesn't already have, so there's " +
					"no pull request. If the issue is already done or needs no code, close it or remove the Sandcastle label; " +
					"otherwise every run builds it again.",
			);
		}

		// Review. The reviewer may commit refinements, and must end with a
		// verdict; anything but an approval keeps the branch from being published.
		let verdict: Verdict;
		try {
			const review = await sandbox.run({
				name: "reviewer",
				agent: sandcastle.claudeCode(MODEL),
				maxIterations: 1,
				promptFile: "./.sandcastle/review-prompt.md",
				promptArgs,
			});
			verdict = parseVerdict(review.stdout);
		} catch (error) {
			verdict = { approved: false, summary: `The reviewer failed: ${error}` };
		}
		log(`reviewer ${verdict.approved ? "approved" : "rejected"}`);
		if (!verdict.approved) {
			return stop("rejected", `Sandcastle's reviewer rejected this issue's change, so ${notPushed}\n\n${fenced(verdict.summary)}`);
		}
		// Uncommitted edits count as a change too: the check fails on them, so
		// a fix the reviewer describes but didn't commit is never published
		// without it.
		if ((await headOf(sandbox)) !== checked || (await isDirty(sandbox))) {
			checked = await check("after the reviewer's changes");
			if (!checked) return { outcome: "check-failed" };
		}

		// Bring in main as it is now, so the PR is up to date (the ruleset
		// requires it) and the check covers the merged result. A conflict is
		// left for a person: the merge is aborted and nothing is pushed.
		main = host.fetchMain();
		const merge = await sandbox.exec(`git merge --no-edit -m "chore: Merge main" ${main} 2>&1`);
		if (merge.exitCode !== 0) {
			await sandbox.exec("git merge --abort");
			return stop(
				"conflicts-with-main",
				`Sandcastle's reviewer approved this issue's change, but it doesn't merge cleanly with main, so ${notPushed}\n\n` +
					fenced(tail(merge.stdout, CHECK_COMMENT_LINES)),
			);
		}
		if ((await headOf(sandbox)) !== checked) {
			checked = await check("after merging main");
			if (!checked) return { outcome: "check-failed" };
		}

		try {
			const prUrl = host.publish(branch, checked, prTitle(issue), prBody(issue, verdict.summary));
			log(`published ${prUrl}`);
			return { outcome: "published", prUrl };
		} catch (error) {
			return stop("publish-failed", `Sandcastle couldn't publish \`${branch}\`: ${error}`);
		}
	} finally {
		// Closing removes the worktree with git on the host, which would read a
		// rewritten config. Leave the sandbox for a person to inspect instead.
		if (host.gitConfigIntact(sandbox.worktreePath)) {
			await sandbox.close();
		} else {
			log("left the sandbox running: the git config changed");
		}
	}
}
