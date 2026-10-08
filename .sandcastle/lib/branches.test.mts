import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { describe, it } from "node:test";
import { branchFor, isIssueBranch, parseHeads, prepareBranches, slugFor, withoutOpenPullRequests } from "./branches.mts";

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
	it("matches the issue's feature, fix and hotfix branches, not another issue's", () => {
		assert.ok(isIssueBranch("feature/4-add-search", 4));
		assert.ok(isIssueBranch("fix/4-stop-the-crash", 4));
		assert.ok(isIssueBranch("hotfix/4-stop-the-crash", 4));
		assert.ok(!isIssueBranch("fix/42-stop-the-crash", 4));
		assert.ok(!isIssueBranch("feature/42-add-search", 4));
		assert.ok(!isIssueBranch("chore/4-add-search", 4));
	});
});

describe("branchFor", () => {
	it("names a bug's branch fix/{n}-{slug}", () => {
		assert.equal(branchFor(issue(7, "fix: Stop the crash", ["Sandcastle", "bug"]), []), "fix/7-stop-the-crash");
	});

	it("names any other issue's branch feature/{n}-{slug}", () => {
		assert.equal(branchFor(issue(8, "feat: Add search"), []), "feature/8-add-search");
	});

	it("reuses the issue's existing branch after its title or labels change", () => {
		const existing = ["feature/80-other-work", "feature/8-add-search"];
		assert.equal(branchFor(issue(8, "feat: Add full-text search", ["Sandcastle", "bug"]), existing), "feature/8-add-search");
	});

	it("reuses a bug's in-flight fix/ or hotfix/ branch", () => {
		assert.equal(branchFor(issue(7, "fix: Stop the crash", ["Sandcastle", "bug"]), ["fix/7-stop-the-crash"]), "fix/7-stop-the-crash");
		assert.equal(
			branchFor(issue(7, "fix: Stop the crash", ["Sandcastle", "bug"]), ["hotfix/7-stop-the-crash"]),
			"hotfix/7-stop-the-crash",
		);
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

describe("withoutOpenPullRequests", () => {
	it("holds back an issue whose branch has an open pull request", () => {
		const { ready, inReview } = withoutOpenPullRequests([issue(4, "A"), issue(5, "B")], ["fix/5-b", "feature/40-other"]);
		assert.deepEqual(ready.map((i) => i.number), [4]);
		assert.deepEqual(inReview.map((i) => i.number), [5]);
	});
});

describe("parseHeads", () => {
	it("reads branch names from ls-remote output", () => {
		assert.deepEqual(parseHeads("abc\trefs/heads/feature/4-a\ndef\trefs/heads/fix/5-b\n"), ["feature/4-a", "fix/5-b"]);
	});
});

describe("prepareBranches", () => {
	// A clone and origin with the given branch commits; ancestry is by
	// commit-name prefix, so "a" is an ancestor of "ab".
	const branchGit = (remote: Record<string, string>, local: Record<string, string>) => {
		const calls = { fetched: [] as string[], set: [] as string[] };
		return {
			calls,
			git: {
				remoteIssueBranches: () => Object.keys(remote),
				localIssueBranches: () => Object.keys(local),
				fetch: (branch: string) => {
					calls.fetched.push(branch);
					return remote[branch]!;
				},
				localSha: (branch: string) => local[branch],
				isAncestor: (ancestor: string, descendant: string) => descendant.startsWith(ancestor),
				setLocal: (branch: string, sha: string) => {
					calls.set.push(`${branch}@${sha}`);
					local[branch] = sha;
				},
			},
		};
	};

	it("creates the local branch from origin's when there's none here", () => {
		const { git, calls } = branchGit({ "feature/4-search": "ab" }, {});
		const { work } = prepareBranches([issue(4, "Add search")], git);
		assert.deepEqual(work.map((w) => w.branch), ["feature/4-search"]);
		assert.deepEqual(calls.fetched, ["feature/4-search"]);
		assert.deepEqual(calls.set, ["feature/4-search@ab"]);
	});

	it("fast-forwards a local branch that's behind origin's", () => {
		const { git, calls } = branchGit({ "feature/4-search": "abc" }, { "feature/4-search": "a" });
		const { work } = prepareBranches([issue(4, "Add search")], git);
		assert.equal(work.length, 1);
		assert.deepEqual(calls.set, ["feature/4-search@abc"]);
	});

	it("keeps a local branch that's ahead of origin's or the same", () => {
		for (const localSha of ["abc", "ab"]) {
			const { git, calls } = branchGit({ "feature/4-search": "ab" }, { "feature/4-search": localSha });
			assert.equal(prepareBranches([issue(4, "Add search")], git).work.length, 1);
			assert.deepEqual(calls.set, []);
		}
	});

	it("skips an issue whose local branch has diverged from origin's", () => {
		const { git, calls } = branchGit({ "feature/4-search": "ab" }, { "feature/4-search": "ax" });
		const { work, skipped } = prepareBranches([issue(4, "Add search"), issue(5, "Sort")], git);
		assert.deepEqual(work.map((w) => w.issue.number), [5]);
		assert.match(skipped[0]!.reason, /diverged/);
		assert.deepEqual(calls.set, []);
	});

	it("reuses a local branch that was never pushed, without fetching", () => {
		const { git, calls } = branchGit({}, { "feature/4-add-search": "a" });
		const { work } = prepareBranches([issue(4, "Renamed title")], git);
		assert.deepEqual(work.map((w) => w.branch), ["feature/4-add-search"]);
		assert.deepEqual(calls.fetched, []);
	});

	it("names a new branch when the issue has none", () => {
		const { git, calls } = branchGit({ "feature/40-x": "a" }, {});
		const { work } = prepareBranches([issue(4, "Add search", ["bug"])], git);
		assert.deepEqual(work.map((w) => w.branch), ["fix/4-add-search"]);
		assert.deepEqual(calls.fetched, []);
	});
});
