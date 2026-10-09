import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { describe, it } from "node:test";
import { prBody, prTitle } from "./publish.mts";

// The PR title standard, from the script the required PR title check runs.
const passesTitleCheck = (title: string) => {
	try {
		execFileSync("bash", ["scripts/check-pr-title.sh", title], { stdio: "ignore" });
		return true;
	} catch {
		return false;
	}
};

describe("prTitle", () => {
	it("keeps a title already in commit format", () => {
		assert.equal(prTitle({ title: "feat(Web): Let users sort by date", labels: [] }), "feat(Web): Let users sort by date");
	});

	it("prefixes fix: for a bug and feat: otherwise", () => {
		assert.equal(prTitle({ title: "Crash on empty list", labels: ["bug"] }), "fix: Crash on empty list");
		assert.equal(prTitle({ title: "Sort by date", labels: ["Sandcastle"] }), "feat: Sort by date");
	});

	it("capitalizes the summary and drops a closing period", () => {
		assert.equal(prTitle({ title: "fix: stop the crash.", labels: [] }), "fix: Stop the crash");
		assert.equal(prTitle({ title: "add search...", labels: [] }), "feat: Add search");
	});

	it("leaves no whitespace before a dropped period", () => {
		assert.equal(prTitle({ title: "feat(Web): Add search .", labels: [] }), "feat(Web): Add search");
		assert.equal(prTitle({ title: "Add search . .", labels: [] }), "feat: Add search");
	});

	it("only produces titles that pass the PR title check", () => {
		for (const title of ["feat: Add search", "fix(Web): stop the crash.", "add search", "  Odd   spacing ", "chore!: drop it", "...", "fix: Trailing space .", "Two . ."]) {
			for (const labels of [[], ["bug"]]) {
				const pr = prTitle({ title, labels });
				assert.ok(passesTitleCheck(pr), `${pr} fails scripts/check-pr-title.sh`);
			}
		}
	});
});

describe("prBody", () => {
	it("keeps closing keywords and mentions in the review inside a fence", () => {
		const body = prBody({ number: 42, title: "Add search" }, "Fixes #12, cc @someone");
		assert.ok(body.includes("```text\nFixes #12, cc @someone\n```"));
		assert.ok(body.endsWith("\n\nFixes #42"));
	});

	it("uses the template's headings, fences the review and closes the issue", () => {
		const body = prBody({ number: 42, title: "Add search" }, "Clean.\nTested.");
		for (const heading of ["## Why", "## What changed", "## Verification"]) assert.ok(body.includes(heading), heading);
		assert.ok(body.includes("```text\nClean.\nTested.\n```"));
		assert.ok(body.endsWith("Fixes #42"));
	});
});
