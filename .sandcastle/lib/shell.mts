import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";

// Sandcastle mounts the clone's .git directory read-write into every sandbox,
// because the agents' commits have to land in it. So an agent can rewrite
// .git/config: a credential.helper, core.sshCommand, core.fsmonitor or
// remote URL there would run commands on the host, or send pushes and gh
// calls elsewhere, the next time the host runs git or gh. The guard records
// the config files once, before any sandbox exists, and every host git and gh
// call first checks they haven't changed.
export class GitConfigGuard {
	private readonly files: readonly string[];
	private readonly digest: string;

	constructor(files: readonly string[]) {
		this.files = files;
		this.digest = this.current();
	}

	private current(): string {
		const hash = createHash("sha256");
		for (const file of this.files) {
			hash.update(`${file}\0${existsSync(file) ? readFileSync(file).toString("base64") : "<missing>"}\0`);
		}
		return hash.digest("hex");
	}

	// Throws when a config file changed since the guard was made.
	verify(): void {
		if (this.current() !== this.digest) {
			tampered = true;
			throw new Error(
				`Git config changed while Sandcastle ran (${this.files.join(", ")}). An agent may have rewritten it, so the ` +
					"host runs no more git or gh commands. Review the file by hand before running Sandcastle again.",
			);
		}
	}
}

let guard: GitConfigGuard | undefined;
let tampered = false;

// Whether a config change was found. A sandbox is then left running rather
// than closed, because closing it runs git on the host.
export const gitConfigTampered = () => tampered;

// Record the clone's git config. main.mts calls this before it creates any
// sandbox; host git and gh calls refuse to run until it has.
export function trustGitConfig(cwd = process.cwd()): void {
	const commonDir = resolve(cwd, execFileSync("git", ["rev-parse", "--git-common-dir"], { cwd, encoding: "utf8" }).trim());
	guard = new GitConfigGuard([join(commonDir, "config"), join(commonDir, "config.worktree")]);
}

// Whether the config is still as recorded; false (and tampered) otherwise.
export function gitConfigIntact(): boolean {
	try {
		verifyGitConfig();
		return true;
	} catch {
		return false;
	}
}

function verifyGitConfig(): void {
	if (!guard) throw new Error("trustGitConfig() must run before the host runs git or gh.");
	guard.verify();
}

// Run a command on the host in `cwd` and return trimmed stdout. Throws on failure.
export const sh = (cwd: string, cmd: string, ...args: string[]) =>
	execFileSync(cmd, args, { cwd, encoding: "utf8", stdio: ["ignore", "pipe", "inherit"] }).trim();

// Run gh on the host, once the git config it reads the remotes from is known
// to be unchanged. `input` is piped to stdin.
export function gh(args: readonly string[], input?: string): string {
	verifyGitConfig();
	return execFileSync("gh", args, {
		cwd: process.cwd(),
		encoding: "utf8",
		stdio: [input === undefined ? "ignore" : "pipe", "pipe", "inherit"],
		input,
	}).trim();
}

// Run git on the host, once the config is known to be unchanged, with hooks
// switched off. The repo's core.hooksPath is .github/hooks, a folder the agents
// can edit in their worktree, and fetch and push run hooks
// (reference-transaction, pre-push): a hook the host ran would be agent-written
// code running outside the sandbox. The full gate those hooks would run is
// CI's job.
export function git(cwd: string, ...args: string[]): string {
	verifyGitConfig();
	return sh(cwd, "git", "-c", "core.hooksPath=/dev/null", ...args);
}
