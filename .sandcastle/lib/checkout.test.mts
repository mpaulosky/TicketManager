import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { describe, it } from "node:test";
import { isPrimaryCheckout, runStamp, worktreeSetup } from "./checkout.mts";

describe("isPrimaryCheckout", () => {
	it("is true when the git dir is the common one", () => {
		assert.equal(isPrimaryCheckout("/repo/.git", "/repo/.git", ""), true);
	});

	it("is false in a linked worktree", () => {
		assert.equal(isPrimaryCheckout("/repo/.git/worktrees/fix-1-x", "/repo/.git", ""), false);
	});

	it("is false when the clone sets baseline.allowPrimaryCommits", () => {
		assert.equal(isPrimaryCheckout("/repo/.git", "/repo/.git", "true"), false);
	});
});

describe("worktreeSetup", () => {
	it("puts a new worktree beside the clone and carries over node_modules and .env", () => {
		assert.deepEqual(worktreeSetup("/home/me/TicketManager", "202610082245"), [
			"git fetch origin",
			"git worktree add -b chore/sandcastle-run-202610082245 '/home/me/TicketManager-worktrees/chore-sandcastle-run-202610082245' origin/main",
			"cd '/home/me/TicketManager-worktrees/chore-sandcastle-run-202610082245'",
			"pnpm install --frozen-lockfile",
			"cp '/home/me/TicketManager/.sandcastle/.env' .sandcastle/.env",
			"pnpm run sandcastle",
		]);
	});

	it("quotes paths with spaces and quotes", () => {
		const [, add, cd] = worktreeSetup("/home/me/My Projects/Ann's TicketManager", "202610082245");
		assert.equal(cd, "cd '/home/me/My Projects/Ann'\\''s TicketManager-worktrees/chore-sandcastle-run-202610082245'");
		assert.match(add!, / '\/home\/me\/My Projects\/Ann'\\''s TicketManager-worktrees\//);
	});

	it("names a branch that passes the branch standard", () => {
		const branch = worktreeSetup("/home/me/TicketManager", runStamp(new Date("2026-10-08T22:45:09Z")))[1]!.split(" ")[4]!;
		assert.equal(branch, "chore/sandcastle-run-202610082245");
		execFileSync("bash", ["scripts/check-branch-name.sh", branch], { stdio: "ignore" });
	});
});
