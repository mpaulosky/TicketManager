import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { isPrimaryCheckout } from "./checkout.mts";

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
