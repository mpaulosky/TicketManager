// Branch naming and fetching. Branch names come from code, never from a model,
// so re-planning an issue always lands on the branch that holds its earlier
// work, and every name passes scripts/check-branch-name.sh.

import { PLANNER_BRANCH } from "./config.mts";
import { git, trustedCommonDir, worktreeIntact } from "./shell.mts";

const maxSlugLength = 50;

// The issue-branch prefixes from docs/PROCESS.md. Sandcastle creates feature/
// and fix/ branches; a hotfix/ branch someone made by hand is still reused.
const issuePrefixes = ["feature", "fix", "hotfix"] as const;

// The parts of an issue its branch name depends on.
export type BranchIssue = { number: number; title: string; labels: string[] };

// The issue title as a branch slug: no conventional-commit prefix, lower case,
// apostrophes dropped so "haven't" stays one word, and runs of ASCII letters
// and digits joined with "-", cut to 50 characters at a "-" boundary.
export function slugFor(title: string): string {
	const words = title
		.replace(/^\s*[a-z]+(?:\([^)]*\))?!?:\s*/i, "")
		.toLowerCase()
		.replace(/['’]/g, "")
		.match(/[a-z0-9]+/g) ?? [];
	const slug = words.join("-");
	if (slug.length === 0) return "issue";
	if (slug.length <= maxSlugLength) return slug;

	const cut = slug.slice(0, maxSlugLength + 1);
	const boundary = cut.lastIndexOf("-");
	return boundary > 0 ? cut.slice(0, boundary) : slug.slice(0, maxSlugLength);
}

// Whether a branch belongs to the issue: feature/{n}-*, fix/{n}-* or hotfix/{n}-*.
export function isIssueBranch(branch: string, issueNumber: number): boolean {
	return issuePrefixes.some((prefix) => branch.startsWith(`${prefix}/${issueNumber}-`));
}

// The issue's branch: its existing feature/, fix/ or hotfix/{n}-* branch when
// there is one, even if the title or labels have changed since, so earlier
// work is built on rather than redone. Otherwise fix/{n}-{slug} for a bug and
// feature/{n}-{slug} for everything else.
export function branchFor(issue: BranchIssue, existingBranches: readonly string[]): string {
	const existing = existingBranches
		.filter((branch) => isIssueBranch(branch, issue.number))
		.sort()[0];
	if (existing) return existing;

	const prefix = issue.labels.includes("bug") ? "fix" : "feature";
	return `${prefix}/${issue.number}-${slugFor(issue.title)}`;
}

// Hold back every issue that already has an open pull request from one of its
// branches: its work is waiting for review, and building it again would only
// pile commits onto that PR. Pass only same-repository PRs' branches (see
// sameRepoPullRequests).
export function withoutOpenPullRequests<T extends BranchIssue>(
	issues: readonly T[],
	openPrBranches: readonly string[],
): { ready: T[]; inReview: T[] } {
	const ready: T[] = [];
	const inReview: T[] = [];
	for (const issue of issues) {
		(openPrBranches.some((branch) => isIssueBranch(branch, issue.number)) ? inReview : ready).push(issue);
	}
	return { ready, inReview };
}

// Branch names from `git ls-remote --heads` output, without refs/heads/.
export function parseHeads(lsRemote: string): string[] {
	return lsRemote
		.split("\n")
		.filter(Boolean)
		.map((line) => line.split("\t")[1]!.replace(/^refs\/heads\//, ""));
}

// The git operations prepareBranches needs; tests pass a stub.
export type BranchGit = {
	// The issue branches on origin.
	remoteIssueBranches(): string[];
	// The issue branches in this clone, which keep the commits of work that
	// was never published.
	localIssueBranches(): string[];
	// Fetch origin's branch into its remote-tracking ref, and return its commit.
	fetch(branch: string): string;
	// The local branch's commit, or undefined when there's no local branch.
	localSha(branch: string): string | undefined;
	// Whether `ancestor` is an ancestor of (or the same as) `descendant`.
	isAncestor(ancestor: string, descendant: string): boolean;
	// Create the local branch at `sha`, or move it there.
	setLocal(branch: string, sha: string): void;
};

const hostGit: BranchGit = {
	remoteIssueBranches: () =>
		parseHeads(
			git(process.cwd(), "ls-remote", "--heads", "origin", ...issuePrefixes.map((prefix) => `refs/heads/${prefix}/*`)),
		),
	localIssueBranches: () =>
		git(process.cwd(), "for-each-ref", "--format=%(refname:short)", ...issuePrefixes.map((prefix) => `refs/heads/${prefix}/`))
			.split("\n")
			.filter(Boolean),
	fetch: (branch) => {
		git(process.cwd(), "fetch", "--quiet", "origin", `+refs/heads/${branch}:refs/remotes/origin/${branch}`);
		return git(process.cwd(), "rev-parse", `refs/remotes/origin/${branch}^{commit}`);
	},
	localSha: (branch) => {
		try {
			return git(process.cwd(), "rev-parse", "--quiet", "--verify", `refs/heads/${branch}^{commit}`);
		} catch {
			return undefined;
		}
	},
	isAncestor: (ancestor, descendant) => {
		try {
			git(process.cwd(), "merge-base", "--is-ancestor", ancestor, descendant);
			return true;
		} catch {
			return false;
		}
	},
	setLocal: (branch, sha) => {
		git(process.cwd(), "branch", "--force", branch, sha);
	},
};

// Main's commit on origin, fetched so its objects are here: the base of every
// new issue branch, of every diff the reviewer reads, of the check script and
// of the merge each branch gets before it's published. The id comes from
// origin itself (ls-remote), never from refs/remotes/origin/main, which lives
// in the .git the sandboxes can write. The host's git calls are synchronous,
// so two fetches never contend for a ref's lock.
export function fetchMain(): string {
	const [sha] = git(process.cwd(), "ls-remote", "origin", "refs/heads/main").split(/\s/);
	if (!sha || !/^[0-9a-f]{40,64}$/.test(sha)) throw new Error("Couldn't read main's commit from origin.");
	git(process.cwd(), "fetch", "--quiet", "origin", "main");
	git(process.cwd(), "cat-file", "-e", `${sha}^{commit}`);
	return sha;
}

// Move the planner's branch to main's commit before each plan. Sandcastle
// starts a branch from its base only when the branch is new, so without this
// every planner would read the repository as it was when the branch was made,
// along with anything an earlier planner committed there.
// A planner worktree an interrupted run left behind still has the branch
// checked out, which would make the reset fail, so it's removed first, once
// its git files show it's safe to run git on.
export function resetPlannerBranch(mainSha: string): void {
	const leftover = worktreeForBranch(git(process.cwd(), "worktree", "list", "--porcelain"), PLANNER_BRANCH);
	if (leftover !== undefined) {
		if (!worktreeIntact(leftover, trustedCommonDir())) {
			throw new Error(
				`The planner's leftover worktree at ${leftover} has changed git files, so Sandcastle won't run git on it. ` +
					"Inspect it, then delete the folder and run `git worktree prune`.",
			);
		}
		console.warn(`  Removing the planner's leftover worktree at ${leftover}.`);
		git(process.cwd(), "worktree", "remove", "--force", leftover);
	}
	git(process.cwd(), "branch", "--force", PLANNER_BRANCH, mainSha);
}

// The path of the worktree that has `branch` checked out, from
// `git worktree list --porcelain` output, or undefined when none has.
export function worktreeForBranch(porcelain: string, branch: string): string | undefined {
	for (const entry of porcelain.split(/\n\n+/)) {
		const lines = entry.split("\n");
		if (lines.includes(`branch refs/heads/${branch}`)) {
			return lines.find((line) => line.startsWith("worktree "))?.slice("worktree ".length);
		}
	}
	return undefined;
}

// The commits `sha` has that `mainSha` doesn't.
export function commitsAhead(mainSha: string, sha: string): number {
	return Number(git(process.cwd(), "rev-list", "--count", `${mainSha}..${sha}`));
}

export type PreparedBranches<T> = {
	work: { issue: T; branch: string }[];
	// Issues not built this round, and why.
	skipped: { issue: T; branch: string; reason: string }[];
};

// Name each issue's branch, reusing one that already exists here or on
// origin. A branch on origin is fetched, and the local branch is created or
// fast-forwarded to it, so the sandbox (which checks out the local branch)
// starts from the work already pushed, and the host's push stays a
// fast-forward. A local branch that has diverged from origin's is left for a
// person rather than built on.
export function prepareBranches<T extends BranchIssue>(issues: readonly T[], branchGit: BranchGit = hostGit): PreparedBranches<T> {
	const remoteBranches = branchGit.remoteIssueBranches();
	const knownBranches = [...new Set([...remoteBranches, ...branchGit.localIssueBranches()])];
	const prepared: PreparedBranches<T> = { work: [], skipped: [] };
	for (const issue of issues) {
		const branch = branchFor(issue, knownBranches);
		if (remoteBranches.includes(branch)) {
			try {
				const remote = branchGit.fetch(branch);
				const local = branchGit.localSha(branch);
				if (local === undefined || (local !== remote && branchGit.isAncestor(local, remote))) {
					branchGit.setLocal(branch, remote);
				} else if (!branchGit.isAncestor(remote, local)) {
					prepared.skipped.push({
						issue,
						branch,
						reason: `the local \`${branch}\` and origin's have diverged; reconcile them by hand`,
					});
					continue;
				}
			} catch (error) {
				prepared.skipped.push({ issue, branch, reason: `\`${branch}\` couldn't be brought up to date with origin's: ${error}` });
				continue;
			}
		}
		prepared.work.push({ issue, branch });
	}
	return prepared;
}
