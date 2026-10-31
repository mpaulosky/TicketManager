import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { describe, it } from "node:test";

const root = new URL("../", import.meta.url);
const read = (path: string) => readFileSync(new URL(path, root), "utf8");

// Files Apply writes once and never overwrites, which this repository keeps by hand.
describe("Seed files kept by hand", () => {
  it("package.json defines the sandcastle:status script that .sandcastle/status.mts documents", () => {
    const scripts = JSON.parse(read("package.json")).scripts as Record<string, string>;
    assert.equal(scripts["sandcastle:status"], "tsx .sandcastle/status.mts");
  });

  it("check.sh unsets the GIT_* variables a git hook sets before it runs anything", () => {
    const script = read(".sandcastle/check.sh");
    const unset = script.indexOf("unset GIT_DIR GIT_WORK_TREE GIT_INDEX_FILE GIT_PREFIX");
    assert.notEqual(unset, -1);
    assert.ok(unset < script.indexOf("git rev-parse"), "the unset must come before the first git command");
  });
});
