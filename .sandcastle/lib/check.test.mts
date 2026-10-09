import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { checkCommand, fenced, headOf, isDirty, runCheck, tail } from "./check.mts";

const main = "c".repeat(40);
const CHECK_COMMAND = checkCommand(main);

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
				[CHECK_COMMAND]: { stdout: "\u001b[32mok\u001b[0m\n", exitCode: 0 },
				"git status --porcelain 2>&1": { stdout: "", exitCode: 0 },
			}),
			main,
		);
		assert.deepEqual(result, { passed: true, output: "ok\n", head: shaA });
	});

	it("fails a red check", async () => {
		const result = await runCheck(
			sandboxWith({ "git rev-parse HEAD": head(shaA), [CHECK_COMMAND]: { stdout: "error CS1002", exitCode: 1 } }),
			main,
		);
		assert.equal(result.passed, false);
		assert.equal(result.output, "error CS1002");
	});

	it("fails a green check over uncommitted changes", async () => {
		const result = await runCheck(
			sandboxWith({
				"git rev-parse HEAD": head(shaA),
				[CHECK_COMMAND]: { stdout: "ok", exitCode: 0 },
				"git status --porcelain 2>&1": { stdout: " M src/Web/Program.cs\n", exitCode: 0 },
			}),
			main,
		);
		assert.equal(result.passed, false);
		assert.match(result.output, /uncommitted changes/);
	});

	it("fails a green check when HEAD moved while it ran", async () => {
		const result = await runCheck(
			sandboxWith({
				"git rev-parse HEAD": [head(shaA), head(shaB)],
				[CHECK_COMMAND]: { stdout: "ok", exitCode: 0 },
				"git status --porcelain 2>&1": { stdout: "", exitCode: 0 },
			}),
			main,
		);
		assert.equal(result.passed, false);
		assert.match(result.output, /HEAD moved/);
	});
});

describe("checkCommand", () => {
	it("runs main's copy of check.sh by commit id, not a ref an agent could move", () => {
		assert.ok(CHECK_COMMAND.includes(`git show ${main}:.sandcastle/check.sh`));
		assert.doesNotMatch(CHECK_COMMAND, /origin\/main/);
	});

	it("runs with only the image's root-owned directories on PATH", () => {
		assert.match(CHECK_COMMAND, /^export PATH=\/usr\/local\/bin:\/usr\/bin:\/bin;/);
	});

	it("refuses anything but a commit id", () => {
		assert.throws(() => checkCommand("origin/main"), /Not a commit id/);
		assert.throws(() => checkCommand(`${main}; rm -rf /`), /Not a commit id/);
	});
});

describe("isDirty", () => {
	it("is true for uncommitted changes or a failing git status", async () => {
		assert.equal(await isDirty(sandboxWith({ "git status --porcelain 2>&1": { stdout: "", exitCode: 0 } })), false);
		assert.equal(await isDirty(sandboxWith({ "git status --porcelain 2>&1": { stdout: " M a.cs\n", exitCode: 0 } })), true);
		assert.equal(await isDirty(sandboxWith({ "git status --porcelain 2>&1": { stdout: "fatal", exitCode: 128 } })), true);
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
