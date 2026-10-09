import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";

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
let repo: { gitDir: string; commonDir: string } | undefined;
let tampered = false;

// Whether a config change was found. A sandbox is then left running rather
// than closed, because closing it runs git on the host.
export const gitConfigTampered = () => tampered;

// Record the clone's git config and where its git directories are. main.mts
// calls this before it creates any sandbox; host git and gh calls refuse to
// run until it has. The guarded files are the shared config, this checkout's
// own config.worktree (under .git/worktrees/<name>/ for a linked worktree),
// and its commondir file, which would point git at another config. Host git
// and gh also get GIT_DIR and GIT_COMMON_DIR, so git never follows a
// commondir file to find the config.
export function trustGitConfig(cwd = process.cwd()): void {
	const revParse = (...args: string[]) =>
		resolve(cwd, execFileSync("git", ["rev-parse", "--path-format=absolute", ...args], { cwd, encoding: "utf8" }).trim());
	repo = { gitDir: revParse("--absolute-git-dir"), commonDir: revParse("--git-common-dir") };
	guard = new GitConfigGuard([
		...new Set([join(repo.commonDir, "config"), join(repo.gitDir, "config.worktree"), join(repo.gitDir, "commondir")]),
	]);
}

// Whether a sandbox's worktree still points git where Sandcastle made it
// point. Closing a sandbox runs git on the host inside the worktree, which
// reads the worktree's .git file and, through it, a commondir file: both are
// in the sandbox's reach, and either could lead git to a config an agent
// wrote. Git writes "gitdir: <common>/worktrees/<name>" and "../..".
export function worktreeIntact(worktreePath: string, commonDir: string): boolean {
	try {
		const pointer = /^gitdir: (.+)\n?$/.exec(readFileSync(join(worktreePath, ".git"), "utf8"));
		if (!pointer) return false;
		const gitDir = resolve(worktreePath, pointer[1]!);
		if (dirname(gitDir) !== join(commonDir, "worktrees")) return false;
		if (existsSync(join(gitDir, "config.worktree"))) return false;
		return readFileSync(join(gitDir, "commondir"), "utf8").trim() === "../..";
	} catch {
		return false;
	}
}

// Refuse to go on while a worktree an earlier run left under
// .sandcastle/worktrees/ has redirected git files: Sandcastle reuses such a
// worktree for its branch and runs git in it on the host. Throws, naming them.
export function assertLeftoverWorktreesIntact(cwd = process.cwd()): void {
	if (!repo) throw new Error("trustGitConfig() must run first.");
	const dir = join(cwd, ".sandcastle", "worktrees");
	if (!existsSync(dir)) return;
	const bad = readdirSync(dir)
		.map((name) => join(dir, name))
		.filter((path) => existsSync(join(path, ".git")) && !worktreeIntact(path, repo!.commonDir));
	if (bad.length > 0) {
		tampered = true;
		throw new Error(`These worktrees' git files were changed, so Sandcastle won't run git in them: ${bad.join(", ")}. Inspect and remove them by hand.`);
	}
}

// Whether the config is still as recorded and, given a sandbox's worktree,
// that it still points where it should; false (and tampered) otherwise.
export function gitConfigIntact(worktreePath?: string): boolean {
	try {
		verifyGitConfig();
	} catch {
		return false;
	}
	if (worktreePath !== undefined && !worktreeIntact(worktreePath, repo!.commonDir)) {
		tampered = true;
		console.error(`The git files of the worktree at ${worktreePath} changed; an agent may have redirected them.`);
		return false;
	}
	return true;
}

function verifyGitConfig(): NodeJS.ProcessEnv {
	if (!guard || !repo) throw new Error("trustGitConfig() must run before the host runs git or gh.");
	guard.verify();
	return { ...process.env, GIT_DIR: repo.gitDir, GIT_COMMON_DIR: repo.commonDir };
}

// Run a command on the host in `cwd` and return trimmed stdout. Throws on failure.
export const sh = (cwd: string, cmd: string, ...args: string[]) => shWith(process.env, cwd, cmd, ...args);

const shWith = (env: NodeJS.ProcessEnv, cwd: string, cmd: string, ...args: string[]) =>
	execFileSync(cmd, args, { cwd, env, encoding: "utf8", stdio: ["ignore", "pipe", "inherit"] }).trim();

// Run gh on the host, once the git config it reads the remotes from is known
// to be unchanged. `input` is piped to stdin.
export function gh(args: readonly string[], input?: string): string {
	const env = verifyGitConfig();
	return execFileSync("gh", args, {
		cwd: process.cwd(),
		env,
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
	const env = verifyGitConfig();
	return shWith(env, cwd, "git", "-c", "core.hooksPath=/dev/null", ...args);
}
