// Where Sandcastle may run. The merger commits on the branch checked out
// where main.mts runs (docker()'s default `head` strategy), and a clean
// `git merge` runs no pre-commit hook, so nothing else stops it committing
// onto main in the primary checkout. main.mts refuses to start there.
import { execFileSync } from "node:child_process";

const git = (...args: string[]) => execFileSync("git", args, { encoding: "utf8" }).trim();

// The same test the pre-commit hook makes: in the primary checkout the git
// dir is the common one, unless the clone opts out with
// baseline.allowPrimaryCommits.
export function isPrimaryCheckout(gitDir: string, commonDir: string, allowPrimaryCommits: string): boolean {
	return gitDir === commonDir && allowPrimaryCommits !== "true";
}

export function runningInPrimaryCheckout(): boolean {
	let allow = "";
	try {
		allow = git("config", "--bool", "baseline.allowPrimaryCommits");
	} catch {
		// Unset: git config exits 1.
	}
	return isPrimaryCheckout(
		git("rev-parse", "--absolute-git-dir"),
		git("rev-parse", "--path-format=absolute", "--git-common-dir"),
		allow,
	);
}

// A path or argument quoted for a POSIX shell, so a space or quote in it
// survives a copy and paste.
const quote = (text: string) => `'${text.replaceAll("'", "'\\''")}'`;

// The commands that set up a worktree Sandcastle can run in, beside the
// primary checkout at `root` as docs/PROCESS.md lays them out. node_modules
// and .sandcastle/.env are untracked, so a new worktree has neither. `stamp`
// makes the branch and folder new for each run, so the commands still work
// after an earlier run's branch is left behind by a squash merge.
export function worktreeSetup(root: string, stamp: string): string[] {
	const parent = root.replace(/\/[^/]*$/, "");
	const name = root.slice(parent.length + 1);
	const branch = `chore/sandcastle-run-${stamp}`;
	const worktree = `${parent}/${name}-worktrees/${branch.replace("/", "-")}`;
	return [
		`git fetch origin`,
		`git worktree add -b ${branch} ${quote(worktree)} origin/main`,
		`cd ${quote(worktree)}`,
		`pnpm install --frozen-lockfile`,
		`cp ${quote(`${root}/.sandcastle/.env`)} .sandcastle/.env`,
		`pnpm run sandcastle`,
	];
}

// The current UTC time as yyyymmddhhmm, a branch-slug-safe run stamp.
export function runStamp(now: Date = new Date()): string {
	return now.toISOString().slice(0, 16).replace(/[-T:]/g, "");
}

export function checkoutRoot(): string {
	return git("rev-parse", "--show-toplevel");
}
