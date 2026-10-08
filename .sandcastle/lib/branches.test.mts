import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { describe, it } from "node:test";
import { branchFor, isIssueBranch, slugFor } from "./branches.mts";

const issue = (number: number, title: string, labels: string[] = ["Sandcastle"]) => ({ number, title, labels });

// The branch standard, from the script the pre-push hook and CI share.
const passesBranchStandard = (branch: string) => {
  try {
    execFileSync("bash", ["scripts/check-branch-name.sh", branch], { stdio: "ignore" });
    return true;
  } catch {
    return false;
  }
};

describe("slugFor", () => {
  it("drops the conventional-commit prefix, lower-cases and joins words with hyphens", () => {
    assert.equal(
      slugFor("feat(sandcastle): Hold back issues whose blockers haven't landed"),
      "hold-back-issues-whose-blockers-havent-landed",
    );
  });

  it("drops prefixes without a scope or with a breaking-change mark", () => {
    assert.equal(slugFor("fix: Stop the crash"), "stop-the-crash");
    assert.equal(slugFor("refactor(Domain)!: Rename Result"), "rename-result");
  });

  it("drops curly apostrophes too", () => {
    assert.equal(slugFor("Don’t reuse the cache"), "dont-reuse-the-cache");
  });

  it("joins every run of other characters into one hyphen and trims the ends", () => {
    assert.equal(slugFor("  Add  C# / .NET 10 support!  "), "add-c-net-10-support");
  });

  it("cuts a long title at a hyphen within 50 characters", () => {
    const slug = slugFor("Make the planner hold back every issue whose blockers have not landed on main yet");
    assert.equal(slug, "make-the-planner-hold-back-every-issue-whose");
    assert.ok(slug.length <= 50);
  });

  it("falls back to 'issue' when the title has no letters or digits", () => {
    assert.equal(slugFor("feat: ???"), "issue");
  });
});

describe("isIssueBranch", () => {
  it("matches the issue's feature and hotfix branches, not another issue's", () => {
    assert.ok(isIssueBranch("feature/4-add-search", 4));
    assert.ok(isIssueBranch("hotfix/4-stop-the-crash", 4));
    assert.ok(!isIssueBranch("feature/42-add-search", 4));
    assert.ok(!isIssueBranch("chore/4-add-search", 4));
  });
});

describe("branchFor", () => {
  it("names a bug's branch hotfix/{n}-{slug}", () => {
    assert.equal(branchFor(issue(7, "fix: Stop the crash", ["Sandcastle", "bug"]), []), "hotfix/7-stop-the-crash");
  });

  it("names any other issue's branch feature/{n}-{slug}", () => {
    assert.equal(branchFor(issue(8, "feat: Add search"), []), "feature/8-add-search");
  });

  it("reuses the issue's existing branch after its title or labels change", () => {
    const existing = ["feature/80-other-work", "feature/8-add-search"];
    assert.equal(branchFor(issue(8, "feat: Add full-text search", ["Sandcastle", "bug"]), existing), "feature/8-add-search");
  });

  it("only names branches that pass the branch standard", () => {
    for (const title of ["feat: Add search", "fix: Stop the crash!", "???", "Don’t reuse the cache"]) {
      for (const labels of [["Sandcastle"], ["Sandcastle", "bug"]]) {
        const branch = branchFor(issue(12, title, labels), []);
        assert.ok(passesBranchStandard(branch), `${branch} fails scripts/check-branch-name.sh`);
      }
    }
  });
});
