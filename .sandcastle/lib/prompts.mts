// Prompt arguments built from GitHub content. Agents can't reach GitHub, so
// this is everything they learn about an issue. The issues passed in hold
// only what trusted authors wrote (see trustedIssues).
//
// Issue text is only ever placed in plain {{KEY}} placeholders, never inside
// a !`…` shell block. Sandcastle 0.12 marks the prompt file's own shell blocks
// before it substitutes the arguments and runs only marked blocks, so a title
// containing !`cmd` stays text; a placeholder inside a shell block, though,
// would become part of the command. Only BRANCH and BASE_BRANCH, which the
// host makes, are used in shell blocks.

import { BASE_BRANCH } from "./config.mts";
import type { SandcastleIssue } from "./github.mts";

// The arguments the prompts may use inside a !`…` shell block.
export const SHELL_SAFE_ARGS: ReadonlySet<string> = new Set(["BRANCH", "BASE_BRANCH"]);

// Sandcastle sets {{TARGET_BRANCH}} itself (to the sandbox's own branch inside
// createSandbox) and refuses an override, so the branch to compare against
// goes in as {{BASE_BRANCH}}.
export function issuePromptArgs(issue: SandcastleIssue, branch: string) {
	return {
		TASK_ID: String(issue.number),
		ISSUE_TITLE: issue.title,
		ISSUE_BODY: issue.body || "(no description)",
		ISSUE_COMMENTS: issue.comments.length > 0 ? issue.comments.join("\n\n---\n\n") : "(no comments)",
		BRANCH: branch,
		BASE_BRANCH,
	};
}

export function plannerPromptArgs(ready: readonly SandcastleIssue[]) {
	return { ISSUES_JSON: JSON.stringify(ready) };
}
