// Branch naming and fetching. Branch names come from code, never from a model,
// so re-planning an issue always lands on the branch that holds its earlier
// work, and every name passes scripts/check-branch-name.sh.

import { BASE_BRANCH } from "./config.mts";
import { git } from "./shell.mts";

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
	// Fetch origin's branch into its remote-tracking ref.
	fetch(branch: string): void;
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
	},
};

// Refresh origin/main, the base of every new issue branch, of every diff the
// reviewer reads, and of the merge each branch gets before it's published.
// The host's git calls are synchronous, so two fetches never contend for the
// ref's lock.
export function fetchMain(): void {
	git(process.cwd(), "fetch", "--quiet", "origin", "main");
}

// The commits `sha` has that main doesn't.
export function commitsAhead(sha: string): number {
	return Number(git(process.cwd(), "rev-list", "--count", `${BASE_BRANCH}..${sha}`));
}

// Name each issue's branch, reusing one that already exists here or on
// origin, and fetch the ones on origin, so the sandbox starts from the work
// already pushed rather than from main.
export function prepareBranches<T extends BranchIssue>(issues: readonly T[], branchGit: BranchGit = hostGit): { issue: T; branch: string }[] {
	const remoteBranches = branchGit.remoteIssueBranches();
	const knownBranches = [...new Set([...remoteBranches, ...branchGit.localIssueBranches()])];
	return issues.map((issue) => {
		const branch = branchFor(issue, knownBranches);
		if (remoteBranches.includes(branch)) branchGit.fetch(branch);
		return { issue, branch };
	});
}
