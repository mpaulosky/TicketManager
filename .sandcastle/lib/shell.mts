import { execFileSync } from "node:child_process";

// Run a command on the host in `cwd` and return trimmed stdout. Throws on failure.
export const sh = (cwd: string, cmd: string, ...args: string[]) =>
	execFileSync(cmd, args, { cwd, encoding: "utf8", stdio: ["ignore", "pipe", "inherit"] }).trim();

// Run git on the host with hooks switched off. The repo's core.hooksPath is
// .github/hooks, a folder the agents can edit in their worktree, and fetch and
// push run hooks (reference-transaction, pre-push): a hook the host ran would
// be agent-written code running outside the sandbox. The full gate those hooks
// would run is CI's job.
export const git = (cwd: string, ...args: string[]) => sh(cwd, "git", "-c", "core.hooksPath=/dev/null", ...args);
