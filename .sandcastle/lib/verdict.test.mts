import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { parseVerdict } from "./verdict.mts";

describe("parseVerdict", () => {
	it("reads an approval", () => {
		assert.deepEqual(parseVerdict('Looks good.\n<verdict>{"approved": true, "summary": "Clean and tested."}</verdict>'), {
			approved: true,
			summary: "Clean and tested.",
		});
	});

	it("reads a rejection", () => {
		const verdict = parseVerdict('<verdict>{"approved": false, "summary": "No test covers the empty list."}</verdict>');
		assert.equal(verdict.approved, false);
		assert.equal(verdict.summary, "No test covers the empty list.");
	});

	it("uses the last verdict when there are several", () => {
		const output =
			'<verdict>{"approved": true, "summary": "First look."}</verdict>\n' +
			'<verdict>{"approved": false, "summary": "On reflection, no."}</verdict>';
		assert.equal(parseVerdict(output).approved, false);
	});

	it("accepts JSON inside a code fence", () => {
		assert.equal(parseVerdict('<verdict>\n```json\n{"approved": true, "summary": "Fine."}\n```\n</verdict>').approved, true);
	});

	it("rejects when the verdict is missing, malformed or incomplete", () => {
		for (const output of [
			"All done. <promise>COMPLETE</promise>",
			"<verdict>approved</verdict>",
			'<verdict>{"approved": "yes", "summary": "Fine."}</verdict>',
			'<verdict>{"approved": true}</verdict>',
			'<verdict>{"approved": true, "summary": "  "}</verdict>',
			'<verdict>{"approved": true, "summary": "Unclosed."}',
		]) {
			assert.equal(parseVerdict(output).approved, false, output);
		}
	});
});
