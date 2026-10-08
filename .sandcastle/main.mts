// Parallel Planner with Review: plan → build → review → pull request loop
//
//   Phase 1 (Plan):   The host reads the open Sandcastle issues with its own gh
//                     auth, keeping only issues and comments from the owner,
//                     members and collaborators, and holds back every issue
//                     that already has an open pull request from this
//                     repository. A planner agent picks the ones that can be
//                     built in parallel; the host drops repeated and unknown
//                     ids and names each issue's branch (lib/branches.mts).
//   Phase 2 (Build):  For each issue, in its own sandbox (lib/build.mts): the
//                     implementer works the issue, the host runs
//                     .sandcastle/check.sh, a reviewer refines the change and
//                     returns an approve/reject verdict, the host merges main
//                     in, and checks again whenever HEAD moved. An approved
//                     branch's checked commit is pushed and gets its own draft
//                     PR that fixes the issue; anything else gets a comment on
//                     the issue and isn't pushed. All pipelines run
//                     concurrently.
//
// Nothing is merged into a local branch and no issue is closed here: each
// change reaches main through its PR and the checks in docs/PROCESS.md.
//
// The sandbox gets no GitHub token. Agents read the issue from their prompt,
// and every GitHub write (comments, pushes, PRs) is made by the host, in code.
//
// The loop repeats up to MAX_ITERATIONS times so that newly unblocked issues
// are picked up, and stops early when a round opens no pull request.
//
// Usage (Node strips the types itself, as it does for test:sandcastle):
//   pnpm run sandcastle

import { existsSync, readFileSync } from "node:fs";
import * as sandcastle from "@ai-hero/sandcastle";
import { docker } from "@ai-hero/sandcastle/sandboxes/docker";
import { fetchMain, prepareBranches, withoutOpenPullRequests } from "./lib/branches.mts";
import { buildIssue } from "./lib/build.mts";
import { MAX_ITERATIONS, MODEL } from "./lib/config.mts";
import { commentOnIssue, listSandcastleIssues, openPullRequestBranches, repoName } from "./lib/github.mts";
import { picksFrom, planSchema } from "./lib/plan.mts";
import { plannerPromptArgs } from "./lib/prompts.mts";
import { githubTokensIn } from "./lib/sandbox-env.mts";
import { gitConfigTampered, trustGitConfig } from "./lib/shell.mts";

const envFile = ".sandcastle/.env";
const leakedTokens = existsSync(envFile) ? githubTokensIn(readFileSync(envFile, "utf8")) : [];
if (leakedTokens.length > 0) {
	throw new Error(
		`${envFile} sets ${leakedTokens.join(" and ")}, which Sandcastle would pass into the sandbox. ` +
			"Remove it: the host uses its own gh auth, and agents must not reach GitHub.",
	);
}

// Record the git config and the repository before any sandbox exists; see
// GitConfigGuard and repoName.
trustGitConfig();
repoName();

for (let iteration = 1; iteration <= MAX_ITERATIONS; iteration++) {
	console.log(`\n=== Iteration ${iteration}/${MAX_ITERATIONS} ===\n`);

	// -------------------------------------------------------------------------
	// Phase 1: Plan
	// -------------------------------------------------------------------------
	const { ready, inReview } = withoutOpenPullRequests(listSandcastleIssues(), openPullRequestBranches());
	for (const issue of inReview) {
		console.log(`  ⏸ #${issue.number} is held back: its pull request is open.`);
	}
	if (ready.length === 0) {
		console.log("No open Sandcastle issues ready to build. Exiting.");
		break;
	}

	const plan = await sandcastle.run({
		sandbox: docker(),
		name: "planner",
		// Structured output requires maxIterations: 1.
		maxIterations: 1,
		agent: sandcastle.claudeCode(MODEL),
		promptFile: "./.sandcastle/plan-prompt.md",
		promptArgs: plannerPromptArgs(ready),
		// Throws StructuredOutputError if the tag is missing, the JSON is
		// malformed, or validation fails, which aborts the loop.
		output: sandcastle.Output.object({ tag: "plan", schema: planSchema }),
	});

	const picks = picksFrom(plan.output.issues.map(({ id }) => id), ready);
	if (picks.length === 0) {
		console.log("No unblocked issues to work on. Exiting.");
		break;
	}

	// -------------------------------------------------------------------------
	// Phase 2: Build, review and publish
	// -------------------------------------------------------------------------
	fetchMain();
	const { work, skipped } = prepareBranches(picks);
	for (const { issue, reason } of skipped) {
		console.warn(`  Skipping #${issue.number}: ${reason}.`);
		commentOnIssue(issue.number, `Sandcastle didn't build this issue: ${reason}.`);
	}
	if (work.length === 0) {
		console.log("No issue branch is ready to build. Stopping.");
		break;
	}

	console.log(`Planning complete. ${work.length} issue(s) to build in parallel:`);
	for (const { issue, branch } of work) {
		console.log(`  #${issue.number}: ${issue.title} → ${branch}`);
	}

	// Promise.allSettled means one failing pipeline doesn't cancel the others.
	const settled = await Promise.allSettled(work.map(({ issue, branch }) => buildIssue(issue, branch)));

	const published: string[] = [];
	for (const [i, outcome] of settled.entries()) {
		const { issue, branch } = work[i]!;
		if (outcome.status === "rejected") {
			console.error(`  ✗ #${issue.number} (${branch}) failed: ${outcome.reason}`);
		} else if (outcome.value.prUrl) {
			published.push(`  #${issue.number} (${branch}) → ${outcome.value.prUrl}`);
		}
	}

	if (gitConfigTampered()) {
		throw new Error("The git config changed during the round. Stopping; see the errors above.");
	}

	console.log(`\nRound complete. ${published.length} pull request(s):`);
	for (const line of published) console.log(line);

	if (published.length === 0) {
		// Nothing reached a PR, so the next plan would pick the same issues and
		// repeat the same round. Stop and let a person look at the issue comments.
		console.log("No pull requests opened this round. Stopping.");
		break;
	}
}

console.log("\nAll done.");
