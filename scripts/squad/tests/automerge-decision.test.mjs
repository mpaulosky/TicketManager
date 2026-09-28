// Tests for scripts/squad/automerge-decision.mjs.
// Usage: node --test scripts/squad/tests/
import { test } from "node:test";
import assert from "node:assert/strict";
import { decideAutoMerge } from "../automerge-decision.mjs";

// An open, same-repo, non-draft PR that GitHub is still gathering checks for.
const pr = (overrides = {}) => ({
  state: "OPEN",
  merged: false,
  isDraft: false,
  isSameRepo: true,
  mergeable: "MERGEABLE",
  mergeStateStatus: "BLOCKED",
  autoMergeEnabled: false,
  ...overrides,
});

test("arms auto-merge on a PR still waiting for required checks", () => {
  assert.equal(decideAutoMerge(pr({ mergeStateStatus: "BLOCKED" })).action, "enable");
});

test("merges directly when only optional checks are pending or failing, since GitHub refuses to arm it then", () => {
  assert.equal(decideAutoMerge(pr({ mergeStateStatus: "UNSTABLE" })).action, "merge");
});

test("arms auto-merge on a PR that is behind main", () => {
  assert.equal(decideAutoMerge(pr({ mergeStateStatus: "BEHIND" })).action, "enable");
});

test("arms auto-merge while GitHub is still computing mergeability", () => {
  assert.equal(
    decideAutoMerge(pr({ mergeable: "UNKNOWN", mergeStateStatus: "UNKNOWN" })).action,
    "enable",
  );
});

test("merges directly when the PR is already clean, since GitHub refuses to arm it then", () => {
  assert.equal(decideAutoMerge(pr({ mergeStateStatus: "CLEAN" })).action, "merge");
});

test("skips a PR that already has auto-merge armed", () => {
  assert.equal(decideAutoMerge(pr({ autoMergeEnabled: true })).action, "skip");
});

test("skips a draft PR", () => {
  assert.equal(decideAutoMerge(pr({ isDraft: true })).action, "skip");
});

test("skips a PR from a fork", () => {
  assert.equal(decideAutoMerge(pr({ isSameRepo: false })).action, "skip");
});

test("skips a PR with merge conflicts", () => {
  assert.equal(
    decideAutoMerge(pr({ mergeable: "CONFLICTING", mergeStateStatus: "DIRTY" })).action,
    "skip",
  );
});

test("skips a closed or already-merged PR", () => {
  assert.equal(decideAutoMerge(pr({ state: "CLOSED" })).action, "skip");
  assert.equal(decideAutoMerge(pr({ state: "MERGED", merged: true })).action, "skip");
});

test("gives a reason for every decision", () => {
  for (const status of ["BLOCKED", "CLEAN", "DIRTY"]) {
    const { reason } = decideAutoMerge(pr({ mergeStateStatus: status }));
    assert.ok(reason && reason.length > 0, `missing reason for ${status}`);
  }
});
