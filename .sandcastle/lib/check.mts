// The host's check of an issue branch: .sandcastle/check.sh, run inside the
// sandbox. Its exit code decides whether the branch can be published, never
// what an agent says about it, and the commit it passed on is the one the
// host pushes.

import type { Sandbox } from "@ai-hero/sandcastle";

// The check is main's copy of check.sh, not the branch's, named by the commit
// the host got from origin rather than by origin/main: the agents can edit
// the branch's copy and can move refs in the shared .git, but can't change
// what a commit holds (replace refs, which could, are ignored). PATH is the image's root-owned directories only, so
// the dotnet, pnpm and node it runs aren't ones an agent dropped in its home.
//
// This makes the check hard to sidestep by accident or by a casual
// workaround; it isn't a security boundary. It builds and tests the branch's
// code, which the agents wrote, in a container they've had a shell in, so it
// shows the change builds and its tests pass, not that the change is safe:
// CI and the person who marks the draft PR ready decide that. stderr is
// folded into stdout, so the output keeps the order it was printed in.
export function checkCommand(mainSha: string): string {
	if (!/^[0-9a-f]{40,64}$/.test(mainSha)) throw new Error(`Not a commit id: ${mainSha}`);
	return (
		`export PATH=/usr/local/bin:/usr/bin:/bin; f="$(mktemp)" && ` +
		`{ git --no-replace-objects show ${mainSha}:.sandcastle/check.sh > "$f" && bash "$f"; } 2>&1; s=$?; rm -f "$f"; exit $s`
	);
}

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

// Run the check. A passing check still fails when the worktree is dirty
// (only commits are pushed, so uncommitted edits would be checked but never
// published) or when HEAD moved while it ran (the commit it built isn't the
// one that's there now).
// Whether the worktree has uncommitted changes (or git can't say).
export async function isDirty(sandbox: Pick<Sandbox, "exec">): Promise<boolean> {
	const status = await sandbox.exec("git status --porcelain 2>&1");
	return status.exitCode !== 0 || status.stdout.trim() !== "";
}

export async function runCheck(sandbox: Pick<Sandbox, "exec">, mainSha: string): Promise<CheckRun> {
	const before = await headOf(sandbox);
	if (!before) return { passed: false, output: "git rev-parse HEAD failed, so there's no commit to check." };

	const { stdout, exitCode } = await sandbox.exec(checkCommand(mainSha));
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
