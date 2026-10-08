// The host's check of an issue branch: .sandcastle/check.sh, run inside the
// sandbox. Its exit code decides whether the branch can be published, never
// what an agent says about it, and the commit it passed on is the one the
// host pushes.

import type { Sandbox } from "@ai-hero/sandcastle";

export type CheckRun = { passed: true; output: string; head: string } | { passed: false; output: string };

// The check prints in colour, which only gets in the way of an issue comment.
const ansiEscape = /\u001b\[[0-9;]*[A-Za-z]/g;

// The commit the sandbox's worktree has checked out, or undefined when git
// can't say.
export async function headOf(sandbox: Pick<Sandbox, "exec">): Promise<string | undefined> {
	const { stdout, exitCode } = await sandbox.exec("git rev-parse HEAD");
	const sha = stdout.trim();
	return exitCode === 0 && /^[0-9a-f]{40,64}$/.test(sha) ? sha : undefined;
}

// Run the check with stderr folded into stdout, so the output keeps the order
// it was printed in. A passing check still fails when the worktree is dirty
// (only commits are pushed, so uncommitted edits would be checked but never
// published) or when HEAD moved while it ran (the commit it built isn't the
// one that's there now).
export async function runCheck(sandbox: Pick<Sandbox, "exec">): Promise<CheckRun> {
	const before = await headOf(sandbox);
	if (!before) return { passed: false, output: "git rev-parse HEAD failed, so there's no commit to check." };

	const { stdout, exitCode } = await sandbox.exec(".sandcastle/check.sh 2>&1");
	const output = stdout.replace(ansiEscape, "");
	if (exitCode !== 0) return { passed: false, output };

	const status = await sandbox.exec("git status --porcelain 2>&1");
	if (status.exitCode !== 0) {
		return { passed: false, output: `${output}\ngit status failed, so the worktree can't be shown to be clean:\n${status.stdout}` };
	}
	if (status.stdout.trim()) {
		return { passed: false, output: `${output}\nThe check passed, but the worktree has uncommitted changes:\n${status.stdout}` };
	}
	const after = await headOf(sandbox);
	if (after !== before) {
		return { passed: false, output: `${output}\nThe check passed on ${before}, but HEAD moved to ${after ?? "an unknown commit"} while it ran.` };
	}
	return { passed: true, output, head: before };
}

// The last `lines` lines of the output.
export function tail(output: string, lines: number): string {
	return output.replace(/\n$/, "").split("\n").slice(-lines).join("\n");
}

// Text quoted in a fence longer than any run of backticks inside it.
export function fenced(text: string): string {
	const longestRun = Math.max(0, ...[...text.matchAll(/`+/g)].map((match) => match[0].length));
	const fence = "`".repeat(Math.max(3, longestRun + 1));
	return `${fence}text\n${text}\n${fence}`;
}
