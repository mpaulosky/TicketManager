// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------

// The model every agent runs on.
export const MODEL = "claude-opus-4-8";

// Maximum number of plan → build → publish rounds before stopping.
// Raise this if your backlog is large; lower it for a quick smoke-test run.
export const MAX_ITERATIONS = 10;

// Iterations the implementer gets to finish an issue.
export const IMPLEMENTER_ITERATIONS = 100;

// How much of a failed check's output an issue comment quotes.
export const CHECK_COMMENT_LINES = 100;

// The branch every issue branch starts from, is reviewed against and has
// merged in before it's published. The host fetches it.
export const BASE_BRANCH = "origin/main";

// Hooks run inside the sandbox before the agent starts. pnpm, through
// Corepack at the version package.json's "packageManager" pins, installs
// exactly what pnpm-lock.yaml records, and fails rather than rewrite it. The
// copied node_modules records the host's pnpm store, so pnpm may rebuild it
// against the sandbox's store; confirm-modules-purge=false lets it do that
// without a TTY prompt.
export const hooks = {
	sandbox: {
		onSandboxReady: [{ command: "pnpm install --frozen-lockfile --config.confirm-modules-purge=false" }],
	},
};

// Copy node_modules from the host into the worktree before each sandbox
// starts. Avoids a full install from scratch; the hook above handles
// platform-specific binaries and any packages added since the last copy.
export const copyToWorktree = ["node_modules"];
