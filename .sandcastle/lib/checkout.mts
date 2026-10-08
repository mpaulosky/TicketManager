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
