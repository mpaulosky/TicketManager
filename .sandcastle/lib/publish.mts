// The pull request the host opens for a finished issue. Its title and body
// follow docs/PROCESS.md, so the required PR title check passes and the
// description reads like any other.

import { fenced } from "./check.mts";
import type { SandcastleIssue } from "./github.mts";

const conventionalTitle = /^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([^)]+\))?!?: \S/;

// The issue title when it's already in commit format, otherwise the title
// behind `fix: ` for a bug or `feat: ` for anything else. The summary starts
// with a capital and ends with neither a period nor whitespace, as
// scripts/check-pr-title.sh wants.
export function prTitle(issue: Pick<SandcastleIssue, "title" | "labels">): string {
	const title = issue.title.trim().replace(/\s+/g, " ");
	const match = conventionalTitle.exec(title);
	const prefix = match ? title.slice(0, match[0].length - 1) : `${issue.labels.includes("bug") ? "fix" : "feat"}: `;
	const summary = (match ? title.slice(match[0].length - 1) : title).replace(/[\s.]+$/, "") || "Resolve the issue";
	return `${prefix}${summary.charAt(0).toUpperCase()}${summary.slice(1)}`;
}

export function prBody(issue: Pick<SandcastleIssue, "number" | "title">, reviewSummary: string): string {
	return [
		"## Why",
		"",
		`Issue #${issue.number}: ${issue.title}`,
		"",
		"## What changed",
		"",
		"Sandcastle's implementer built the change in a sandbox, and its reviewer approved it:",
		"",
		// In a fence, so a "Fixes #12" or @mention the model wrote is shown,
		// not acted on.
		fenced(reviewSummary),
		"",
		"## Verification",
		"",
		"- `.sandcastle/check.sh` (the solution build and every test project that needs neither Docker nor a browser)",
		"  passed in the sandbox on the commit this PR was opened with, after main was merged in. The agents had a shell",
		"  in that sandbox, so treat this as a sanity check.",
		"- CI runs the full suite, including the Aspire and Playwright tests.",
		"",
		`Fixes #${issue.number}`,
	].join("\n");
}
