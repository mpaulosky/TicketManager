// Parallel Planner with Review — four-phase orchestration loop
//
// This template drives a multi-phase workflow:
//   Phase 1 (Plan):             An opus agent analyzes open issues, builds a
//                               dependency graph, and outputs a <plan> JSON
//                               listing unblocked issues. The host names
//                               each issue's branch (lib/branches.mts).
//   Phase 2 (Execute + Review): For each issue, a sandbox is created via
//                               createSandbox(). The implementer runs first
//                               (100 iterations). If it signals completion
//                               and produces commits, a reviewer runs in the
//                               same sandbox on the same branch (1 iteration).
//                               All issue pipelines run concurrently via
//                               Promise.allSettled().
//   Phase 3 (Merge):            A single agent merges every branch whose
//                               implementer and reviewer both signalled
//                               completion into the current branch.
//
// The outer loop repeats up to MAX_ITERATIONS times so that newly unblocked
// issues are picked up after each round of merges.
//
// Usage (Node strips the types itself, as it does for test:sandcastle):
//   pnpm run sandcastle
//
// Run it from a linked worktree on its own branch, not the primary checkout,
// which stays on main (docs/PROCESS.md). The merger uses Sandcastle's default
// `head` strategy for docker(), so it merges and commits on the branch checked
// out where this runs. A clean `git merge` runs no pre-commit hook, so main.mts
// checks for itself and refuses to start in the primary checkout, printing the
// commands that set up a worktree: node_modules and .sandcastle/.env are
// untracked, so a new worktree needs `pnpm install --frozen-lockfile` and a
// copy of the primary checkout's .sandcastle/.env.

import * as sandcastle from "@ai-hero/sandcastle";
import { docker } from "@ai-hero/sandcastle/sandboxes/docker";
import { z } from "zod";
import { branchFor, localIssueBranches, openSandcastleIssues } from "./lib/branches.mts";
import { checkoutRoot, runningInPrimaryCheckout, runStamp, worktreeSetup } from "./lib/checkout.mts";

// The planner emits its plan as JSON inside <plan> tags; Output.object extracts
// and validates it against this schema. We use Zod here, but any Standard
// Schema validator works just as well — Valibot, ArkType, etc. See
// https://standardschema.dev.
const planSchema = z.object({
	issues: z.array(
		z.object({ id: z.string(), title: z.string() }),
	),
});

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------

// Maximum number of plan→execute→merge cycles before stopping.
// Raise this if your backlog is large; lower it for a quick smoke-test run.
const MAX_ITERATIONS = 10;

// Hooks run inside the sandbox before the agent starts each iteration.
// pnpm (the version pinned by "packageManager" in package.json, through
// corepack) installs exactly what pnpm-lock.yaml records, and fails rather
// than rewrite the lockfile.
const hooks = {
	sandbox: { onSandboxReady: [{ command: "pnpm install --frozen-lockfile" }] },
};

// Copy node_modules from the host into the worktree before each sandbox
// starts. Avoids a full pnpm install from scratch; the hook above handles
// platform-specific binaries and any packages added since the last copy.
const copyToWorktree = ["node_modules"];

// ---------------------------------------------------------------------------
// Main loop
// ---------------------------------------------------------------------------

if (runningInPrimaryCheckout()) {
	console.error(
		"Run Sandcastle from a linked worktree, not the primary checkout: the merger commits on the branch checked out here.\n" +
			"Set one up and run it there:\n" +
			worktreeSetup(checkoutRoot(), runStamp()).map((command) => `  ${command}`).join("\n"),
	);
	process.exit(1);
}

for (let iteration = 1; iteration <= MAX_ITERATIONS; iteration++) {
	console.log(`\n=== Iteration ${iteration}/${MAX_ITERATIONS} ===\n`);

	// -------------------------------------------------------------------------
	// Phase 1: Plan
	//
	// The planning agent (opus, for deeper reasoning) reads the open issue list,
	// builds a dependency graph, and selects the issues that can be worked in
	// parallel right now (i.e., no blocking dependencies on other open issues).
	//
	// It outputs a <plan> JSON block — Output.object parses and validates it.
	// -------------------------------------------------------------------------
	const plan = await sandcastle.run({
		hooks,
		sandbox: docker(),
		name: "planner",
		// One iteration is enough: the planner just needs to read and reason,
		// not write code. (Structured output requires maxIterations: 1.)
		maxIterations: 1,
		// Opus for planning: dependency analysis benefits from deeper reasoning.
		agent: sandcastle.claudeCode("claude-opus-4-8"),
		promptFile: "./.sandcastle/plan-prompt.md",
		// Extract and validate the <plan> JSON into a typed object. Throws
		// StructuredOutputError if the tag is missing, the JSON is malformed, or
		// validation fails — which aborts the loop.
		output: sandcastle.Output.object({ tag: "plan", schema: planSchema }),
	});

	// Name each picked issue's branch from its number, title and labels on
	// GitHub, so the name follows the branch standard and doesn't drift
	// between plans. An id that isn't an open Sandcastle issue is skipped.
	const openIssues = openSandcastleIssues();
	const existingBranches = localIssueBranches();
	const issues = plan.output.issues.flatMap(({ id }) => {
		const issue = openIssues.find((open) => String(open.number) === id);
		if (!issue) {
			console.warn(`  Skipping ${id}: not an open issue labelled Sandcastle.`);
			return [];
		}
		return [{ id, title: issue.title, branch: branchFor(issue, existingBranches) }];
	});

	if (issues.length === 0) {
		// No unblocked work — either everything is done or everything is blocked.
		console.log("No unblocked issues to work on. Exiting.");
		break;
	}

	console.log(
		`Planning complete. ${issues.length} issue(s) to work in parallel:`,
	);
	for (const issue of issues) {
		console.log(`  ${issue.id}: ${issue.title} → ${issue.branch}`);
	}

	// -------------------------------------------------------------------------
	// Phase 2: Execute + Review
	//
	// For each issue, create a sandbox via createSandbox() so the implementer
	// and reviewer share the same sandbox instance per branch. The implementer
	// runs first; if it signals completion and produces commits, the reviewer
	// runs in the same sandbox.
	//
	// Promise.allSettled means one failing pipeline doesn't cancel the others.
	// -------------------------------------------------------------------------

	const settled = await Promise.allSettled(
		issues.map(async (issue) => {
			const sandbox = await sandcastle.createSandbox({
				branch: issue.branch,
				sandbox: docker(),
				hooks,
				copyToWorktree,
			});

			try {
				// Run the implementer
				const implement = await sandbox.run({
					name: "implementer",
					maxIterations: 100,
					agent: sandcastle.claudeCode("claude-opus-4-8"),
					promptFile: "./.sandcastle/implement-prompt.md",
					promptArgs: {
						TASK_ID: issue.id,
						ISSUE_TITLE: issue.title,
						BRANCH: issue.branch,
					},
				});

				// Only review if the implementer finished: it emits its completion
				// signal when the issue is done. A run that hit maxIterations has
				// no signal, and its commits are partial work.
				if (implement.completionSignal !== undefined && implement.commits.length > 0) {
					const review = await sandbox.run({
						name: "reviewer",
						maxIterations: 1,
						agent: sandcastle.claudeCode("claude-opus-4-8"),
						promptFile: "./.sandcastle/review-prompt.md",
						promptArgs: {
							BRANCH: issue.branch,
						},
					});

					// Merge commits from both runs so the merge phase sees all of them.
					// Each sandbox.run() only returns commits from its own run.
					return {
						completed: review.completionSignal !== undefined,
						commits: [...implement.commits, ...review.commits],
					};
				}

				return { completed: false, commits: implement.commits };
			} finally {
				await sandbox.close();
			}
		}),
	);

	// Log any agents that threw (network error, sandbox crash, etc.).
	for (const [i, outcome] of settled.entries()) {
		if (outcome.status === "rejected") {
			console.error(
				`  ✗ ${issues[i]!.id} (${issues[i]!.branch}) failed: ${outcome.reason}`,
			);
		}
	}

	// Only pass branches whose implementer and reviewer both signalled
	// completion, and that have commits, to the merge phase: the merger closes
	// each issue it merges, so unfinished work must not reach it.
	const completedIssues = settled
		.map((outcome, i) => ({ outcome, issue: issues[i]! }))
		.filter((entry) => {
			if (entry.outcome.status !== "fulfilled") return false;
			const { completed, commits } = entry.outcome.value;
			if (!completed && commits.length > 0) {
				console.warn(
					`  ! ${entry.issue.id} (${entry.issue.branch}) has commits but did not signal completion; not merging.`,
				);
			}
			return completed && commits.length > 0;
		})
		.map((entry) => entry.issue);

	const completedBranches = completedIssues.map((i) => i.branch);

	console.log(
		`\nExecution complete. ${completedBranches.length} completed branch(es) with commits:`,
	);
	for (const branch of completedBranches) {
		console.log(`  ${branch}`);
	}

	if (completedBranches.length === 0) {
		// No branch both finished and made commits — nothing to merge this cycle.
		console.log("No completed branches with commits. Nothing to merge.");
		continue;
	}

	// -------------------------------------------------------------------------
	// Phase 3: Merge
	//
	// One agent merges all completed branches into the current branch,
	// resolving any conflicts and running tests to confirm everything works.
	//
	// The {{BRANCHES}} and {{ISSUES}} prompt arguments are lists that the agent
	// uses to know which branches to merge and which issues to close.
	// -------------------------------------------------------------------------
	await sandcastle.run({
		hooks,
		sandbox: docker(),
		name: "merger",
		maxIterations: 1,
		agent: sandcastle.claudeCode("claude-opus-4-8"),
		promptFile: "./.sandcastle/merge-prompt.md",
		promptArgs: {
			// A markdown list of branch names, one per line.
			BRANCHES: completedBranches.map((b) => `- ${b}`).join("\n"),
			// A markdown list of issue IDs and titles, one per line.
			ISSUES: completedIssues.map((i) => `- ${i.id}: ${i.title}`).join("\n"),
		},
	});

	console.log("\nBranches merged.");
}

console.log("\nAll done.");
