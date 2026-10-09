import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { describe, it } from "node:test";
import { issuePromptArgs, plannerPromptArgs, SHELL_SAFE_ARGS } from "./prompts.mts";

const issue = { number: 5, title: "Add search", body: "", labels: ["Sandcastle"], comments: ["One.", "Two."] };

// The {{KEY}} placeholders a prompt file uses.
const placeholders = (file: string) =>
	new Set([...readFileSync(new URL(`../${file}`, import.meta.url), "utf8").matchAll(/\{\{\s*([A-Za-z_]\w*)\s*\}\}/g)].map((m) => m[1]!));

describe("plannerPromptArgs", () => {
	it("leaves out Sandcastle's own reports", () => {
		const args = plannerPromptArgs([{ ...issue, lastReport: "The check failed." }]);
		assert.doesNotMatch(args.ISSUES_JSON, /The check failed/);
	});
});

describe("issuePromptArgs", () => {
	it("fills in an empty body and joins the comments", () => {
		const args = issuePromptArgs(issue, "feature/5-add-search", "c".repeat(40));
		assert.equal(args.ISSUE_BODY, "(no description)");
		assert.equal(args.ISSUE_COMMENTS, "One.\n\n---\n\nTwo.");
		assert.equal(args.BASE_BRANCH, "c".repeat(40));
	});

	it("never sets a built-in Sandcastle argument, which Sandcastle refuses", () => {
		const args = issuePromptArgs(issue, "feature/5-add-search", "c".repeat(40));
		assert.ok(!("TARGET_BRANCH" in args));
		assert.ok(!("SOURCE_BRANCH" in args));
	});

	// Sandcastle fails a run whose prompt names an argument it wasn't given.
	for (const file of ["implement-prompt.md", "review-prompt.md"]) {
		it(`supplies every placeholder ${file} uses`, () => {
			const args = issuePromptArgs(issue, "feature/5-add-search", "c".repeat(40));
			for (const key of placeholders(file)) assert.ok(key in args, `${file} uses {{${key}}}`);
		});
	}

	it("supplies every placeholder plan-prompt.md uses", () => {
		const args = plannerPromptArgs([issue]);
		for (const key of placeholders("plan-prompt.md")) assert.ok(key in args, `plan-prompt.md uses {{${key}}}`);
	});
});

const prompts = ["implement-prompt.md", "review-prompt.md", "plan-prompt.md"];
const read = (file: string) => readFileSync(new URL(`../${file}`, import.meta.url), "utf8");

describe("prompts", () => {
	// Sandcastle runs a prompt file's !`…` blocks after it fills in the
	// arguments, so an argument inside one becomes part of the command. Issue
	// text must only appear outside them.
	it("use only host-made arguments inside shell blocks", () => {
		for (const file of prompts) {
			for (const [, command] of read(file).matchAll(/!`([^`]+)`/g)) {
				for (const [, key] of command!.matchAll(/\{\{\s*([A-Za-z_]\w*)\s*\}\}/g)) {
					assert.ok(SHELL_SAFE_ARGS.has(key!), `${file} uses {{${key}}} in !\`${command}\``);
				}
			}
		}
	});

	it("point the agents at check.sh, not the full gate", () => {
		for (const file of ["implement-prompt.md", "review-prompt.md"]) {
			assert.match(read(file), /\.sandcastle\/check\.sh/, file);
			assert.doesNotMatch(read(file), /scripts\/gate\.sh/, file);
		}
	});

	it("never ask an agent to use gh or npm", () => {
		for (const file of ["implement-prompt.md", "review-prompt.md", "plan-prompt.md"]) {
			const text = readFileSync(new URL(`../${file}`, import.meta.url), "utf8");
			assert.doesNotMatch(text, /`gh |!`gh|npm run/, file);
		}
	});
});

describe("implement-prompt.md", () => {
	it("asks for commits in the repository's format, not a RALPH: prefix", () => {
		const text = readFileSync(new URL("../implement-prompt.md", import.meta.url), "utf8");
		assert.doesNotMatch(text, /RALPH/);
		assert.match(text, /git-commit-instructions\.md/);
	});
});
