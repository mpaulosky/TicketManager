import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { fenced, headOf, runCheck, tail } from "./check.mts";

const shaA = "a".repeat(40);
const shaB = "b".repeat(40);

type Result = { stdout: string; exitCode: number };

// A sandbox whose exec answers each command from a table. A list answers the
// command once per entry, in order.
const sandboxWith = (results: Record<string, Result | Result[]>) => ({
	exec: async (command: string) => {
		const entry = results[command];
		const result = Array.isArray(entry) ? entry.shift() : entry;
		if (!result) throw new Error(`unexpected command: ${command}`);
		return { ...result, stderr: "" };
	},
});

const head = (sha: string) => ({ stdout: `${sha}\n`, exitCode: 0 });

describe("runCheck", () => {
	it("passes a green check over a clean worktree, without colour codes, and names the commit it checked", async () => {
		const result = await runCheck(
			sandboxWith({
				"git rev-parse HEAD": head(shaA),
				".sandcastle/check.sh 2>&1": { stdout: "\u001b[32mok\u001b[0m\n", exitCode: 0 },
				"git status --porcelain 2>&1": { stdout: "", exitCode: 0 },
			}),
		);
		assert.deepEqual(result, { passed: true, output: "ok\n", head: shaA });
	});

	it("fails a red check", async () => {
		const result = await runCheck(
			sandboxWith({ "git rev-parse HEAD": head(shaA), ".sandcastle/check.sh 2>&1": { stdout: "error CS1002", exitCode: 1 } }),
		);
		assert.equal(result.passed, false);
		assert.equal(result.output, "error CS1002");
	});

	it("fails a green check over uncommitted changes", async () => {
		const result = await runCheck(
			sandboxWith({
				"git rev-parse HEAD": head(shaA),
				".sandcastle/check.sh 2>&1": { stdout: "ok", exitCode: 0 },
				"git status --porcelain 2>&1": { stdout: " M src/Web/Program.cs\n", exitCode: 0 },
			}),
		);
		assert.equal(result.passed, false);
		assert.match(result.output, /uncommitted changes/);
	});

	it("fails a green check when HEAD moved while it ran", async () => {
		const result = await runCheck(
			sandboxWith({
				"git rev-parse HEAD": [head(shaA), head(shaB)],
				".sandcastle/check.sh 2>&1": { stdout: "ok", exitCode: 0 },
				"git status --porcelain 2>&1": { stdout: "", exitCode: 0 },
			}),
		);
		assert.equal(result.passed, false);
		assert.match(result.output, /HEAD moved/);
	});
});

describe("headOf", () => {
	it("reads the commit, or nothing when git fails or prints something else", async () => {
		assert.equal(await headOf(sandboxWith({ "git rev-parse HEAD": head(shaA) })), shaA);
		assert.equal(await headOf(sandboxWith({ "git rev-parse HEAD": { stdout: "fatal: not a git repository", exitCode: 128 } })), undefined);
		assert.equal(await headOf(sandboxWith({ "git rev-parse HEAD": { stdout: "HEAD", exitCode: 0 } })), undefined);
	});
});

describe("tail", () => {
	it("keeps the last lines", () => {
		assert.equal(tail("a\nb\nc\n", 2), "b\nc");
	});
});

describe("fenced", () => {
	it("uses a fence longer than any backtick run inside", () => {
		assert.equal(fenced("x ```` y"), "`````text\nx ```` y\n`````");
		assert.equal(fenced("plain"), "```text\nplain\n```");
	});
});
