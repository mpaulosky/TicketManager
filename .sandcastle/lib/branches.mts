// Branch naming. Branch names come from code, never from a model, so
// re-planning an issue always lands on the branch that holds its earlier work,
// and every name passes scripts/check-branch-name.sh.

import { execFileSync } from "node:child_process";

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

// The local feature/*, fix/* and hotfix/* branches. Sandcastle's branches live in
// this clone, so that's where an issue's earlier work is.
export function localIssueBranches(): string[] {
	return execFileSync(
		"git",
		["for-each-ref", "--format=%(refname:short)", ...issuePrefixes.map((prefix) => `refs/heads/${prefix}/`)],
		{ encoding: "utf8" },
	)
		.split("\n")
		.filter(Boolean);
}

// The open issues labelled Sandcastle, the same list the planner reads.
export function openSandcastleIssues(): BranchIssue[] {
	const json = execFileSync(
		"gh",
		["issue", "list", "--state", "open", "--label", "Sandcastle", "--limit", "100", "--json", "number,title,labels"],
		{ encoding: "utf8" },
	);
	const issues = JSON.parse(json) as { number: number; title: string; labels: { name: string }[] }[];
	return issues.map(({ number, title, labels }) => ({ number, title, labels: labels.map((label) => label.name) }));
}
