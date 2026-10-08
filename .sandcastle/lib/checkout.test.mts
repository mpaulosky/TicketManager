import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { isPrimaryCheckout, worktreeSetup } from "./checkout.mts";

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
	it("puts the worktree beside the clone and carries over node_modules and .env", () => {
		assert.deepEqual(worktreeSetup("/home/me/TicketManager"), [
			"git fetch origin",
			"git worktree add -b chore/sandcastle-run /home/me/TicketManager-worktrees/chore-sandcastle-run origin/main",
			"cd /home/me/TicketManager-worktrees/chore-sandcastle-run",
			"pnpm install --frozen-lockfile",
			"cp /home/me/TicketManager/.sandcastle/.env .sandcastle/.env",
			"pnpm run sandcastle",
		]);
	});
});
