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

// The commands that set up a worktree Sandcastle can run in, beside the
// primary checkout at `root` as docs/PROCESS.md lays them out. node_modules
// and .sandcastle/.env are untracked, so a new worktree has neither.
export function worktreeSetup(root: string): string[] {
	const parent = root.replace(/\/[^/]*$/, "");
	const name = root.slice(parent.length + 1);
	const worktree = `${parent}/${name}-worktrees/chore-sandcastle-run`;
	return [
		`git fetch origin`,
		`git worktree add -b chore/sandcastle-run ${worktree} origin/main`,
		`cd ${worktree}`,
		`pnpm install --frozen-lockfile`,
		`cp ${root}/.sandcastle/.env .sandcastle/.env`,
		`pnpm run sandcastle`,
	];
}

export function checkoutRoot(): string {
	return git("rev-parse", "--show-toplevel");
}
