import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { describe, it } from "node:test";
import { CHECK_PATHS } from "./lib/repo-config.mts";

const root = new URL("../", import.meta.url);
const read = (path: string) => readFileSync(new URL(path, root), "utf8");

// Files Apply writes once and never overwrites, which this repository keeps by hand.
describe("Seed files kept by hand", () => {
  it("package.json defines the sandcastle:status script that .sandcastle/status.mts documents", () => {
    const scripts = JSON.parse(read("package.json")).scripts as Record<string, string>;
    assert.equal(scripts["sandcastle:status"], "tsx .sandcastle/status.mts");
  });

  it("sandbox-gate.sh unsets the GIT_* variables a git hook sets before it runs anything", () => {
    const script = read(".sandcastle/sandbox-gate.sh");
    const unset = script.indexOf("unset GIT_DIR GIT_WORK_TREE GIT_INDEX_FILE GIT_PREFIX");
    assert.notEqual(unset, -1);
    assert.ok(unset < script.indexOf("git rev-parse"), "the unset must come before the first git command");
  });
});

// What the Template's tests read (the local review's script, skills and agent, Claude Review's workflow) must wake the
// Sandcastle check in the gate and in CI: this repo's gate runs .github/ci/sandcastle.sh's list, not .sandcastle/check.sh.
describe("the Sandcastle check watches what the tests read", () => {
  const watched = read(".github/ci/sandcastle.sh");
  for (const path of [
    "scripts/review.sh",
    ".claude/skills/pre-push-review",
    ".claude/skills/hardening-review",
    ".claude/agents/pre-push-reviewer.md",
    ".github/workflows/claude-review.yml",
  ]) {
    it(`SANDCASTLE_PATHS lists ${path}`, () => {
      assert.match(watched, new RegExp(`^  ${path.replace(/[.]/g, "\\.")}$`, "m"));
    });
  }

  it("CHECK_PATHS lists every place the gate reads its checks from", () => {
    for (const path of [".github/scripts/", ".claude/skills/", ".claude/agents/", ".github/hooks/"]) {
      assert.ok(CHECK_PATHS.includes(path), `${path} is missing from CHECK_PATHS`);
    }
  });
});
