import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { buildIssue, type BuildHost, type BuildSandbox } from "./build.mts";
import { checkCommand } from "./check.mts";

const main = "c".repeat(40);

const issue = { number: 7, title: "Add search", body: "Search tickets.", labels: ["Sandcastle"], comments: [] };
const branch = "feature/7-add-search";

const sha = (n: number) => n.toString(16).padStart(40, "0");

// A scripted agent run. moveHead makes it leave HEAD on a new commit (a
// commit, an amend or a reset all look the same from outside).
type RunResult = { completionSignal?: string; stdout?: string; moveHead?: boolean; leaveDirty?: boolean } | Error;

// A pipeline with scripted agent runs, check results and merge result,
// recording what the host was asked to do.
function pipeline(options: {
	implementer?: RunResult;
	reviewer?: RunResult;
	checks?: boolean[];
	merge?: "up-to-date" | "merged" | "conflict";
	ahead?: number;
	publishError?: Error;
	configIntact?: boolean;
}) {
	const checks = [...(options.checks ?? [true, true, true])];
	let head = 1;
	let dirty = false;
	const calls = {
		runs: [] as string[],
		comments: [] as string[],
		published: [] as { sha: string; title: string; body: string }[],
		checkedAt: [] as string[],
		fetches: 0,
		aborted: false,
		closed: false,
	};
	const results: Record<string, RunResult> = {
		implementer: options.implementer ?? { completionSignal: "<promise>COMPLETE</promise>", moveHead: true },
		reviewer: options.reviewer ?? { stdout: '<verdict>{"approved": true, "summary": "Clean."}</verdict>' },
	};
	const ok = (stdout = "") => ({ stdout, stderr: "", exitCode: 0 });

	const sandbox = {
		run: async (opts: { name?: string }) => {
			calls.runs.push(opts.name!);
			const result = results[opts.name!]!;
			if (result instanceof Error) throw result;
			if (result.moveHead) head++;
			if (result.leaveDirty) dirty = true;
			return { iterations: [], stdout: "", commits: [], ...result };
		},
		exec: async (command: string) => {
			if (command === "git rev-parse HEAD") return ok(`${sha(head)}\n`);
			if (command === "git status --porcelain 2>&1") return ok(dirty ? " M src/Web/Program.cs\n" : "");
			if (command === checkCommand(main)) {
				const passed = checks.shift();
				if (passed === undefined) throw new Error("check ran more often than scripted");
				calls.checkedAt.push(sha(head));
				return { stdout: passed ? "ok" : "error CS1002", stderr: "", exitCode: passed ? 0 : 1 };
			}
			if (command.startsWith("git merge --no-edit")) {
				if (options.merge === "conflict") return { stdout: "CONFLICT (content): Merge conflict in a.cs", stderr: "", exitCode: 1 };
				if (options.merge === "merged") head++;
				return ok("Already up to date.");
			}
			if (command === "git merge --abort") {
				calls.aborted = true;
				return ok();
			}
			throw new Error(`unexpected command: ${command}`);
		},
		close: async () => {
			calls.closed = true;
			return {};
		},
	} as unknown as BuildSandbox;

	const host: BuildHost = {
		createSandbox: async () => sandbox,
		fetchMain: () => {
			calls.fetches++;
			return main;
		},
		commitsAhead: () => options.ahead ?? 1,
		commentOnIssue: (_, body) => calls.comments.push(body),
		publish: (_, publishedSha, title, body) => {
			if (options.publishError) throw options.publishError;
			calls.published.push({ sha: publishedSha, title, body });
			return "https://github.com/o/r/pull/1";
		},
		gitConfigIntact: () => options.configIntact ?? true,
		log: () => {},
	};

	return { run: () => buildIssue(issue, branch, main, host), calls, checksLeft: checks, head: () => sha(head) };
}

describe("buildIssue", () => {
	it("publishes the checked commit of an approved branch as a PR that fixes the issue", async () => {
		const { run, calls, head } = pipeline({});
		const result = await run();
		assert.deepEqual(result, { outcome: "published", prUrl: "https://github.com/o/r/pull/1" });
		assert.deepEqual(calls.runs, ["implementer", "reviewer"]);
		assert.equal(calls.published[0]!.sha, head());
		assert.equal(calls.published[0]!.title, "feat: Add search");
		assert.match(calls.published[0]!.body, /Fixes #7/);
		assert.equal(calls.fetches, 1);
		assert.deepEqual(calls.comments, []);
		assert.ok(calls.closed);
	});

	it("checks only once when neither the reviewer nor the merge moves HEAD", async () => {
		const { run, checksLeft } = pipeline({ merge: "up-to-date" });
		assert.equal((await run()).outcome, "published");
		assert.equal(checksLeft.length, 2);
	});

	it("doesn't publish when the reviewer rejects, and says why on the issue", async () => {
		const { run, calls } = pipeline({
			reviewer: { stdout: '<verdict>{"approved": false, "summary": "No test for an empty query."}</verdict>' },
		});
		assert.equal((await run()).outcome, "rejected");
		assert.deepEqual(calls.published, []);
		assert.match(calls.comments[0]!, /No test for an empty query/);
	});

	it("treats a review without a verdict as a rejection", async () => {
		const { run, calls } = pipeline({ reviewer: { stdout: "<promise>COMPLETE</promise>" } });
		assert.equal((await run()).outcome, "rejected");
		assert.deepEqual(calls.published, []);
	});

	it("treats a reviewer that throws as a rejection", async () => {
		const { run, calls } = pipeline({ reviewer: new Error("sandbox crashed") });
		assert.equal((await run()).outcome, "rejected");
		assert.match(calls.comments[0]!, /sandbox crashed/);
	});

	it("stops when the implementer doesn't finish", async () => {
		const { run, calls } = pipeline({ implementer: { moveHead: true } });
		assert.equal((await run()).outcome, "implementer-unfinished");
		assert.deepEqual(calls.runs, ["implementer"]);
		assert.match(calls.comments[0]!, /ran out of iterations/);
	});

	it("stops when the check fails after the implementer", async () => {
		const { run, calls } = pipeline({ checks: [false] });
		assert.equal((await run()).outcome, "check-failed");
		assert.deepEqual(calls.runs, ["implementer"]);
		assert.match(calls.comments[0]!, /error CS1002/);
	});

	it("checks again whenever the reviewer moves HEAD, and stops when that fails", async () => {
		const { run, calls } = pipeline({
			reviewer: { stdout: '<verdict>{"approved": true, "summary": "Tidied."}</verdict>', moveHead: true },
			checks: [true, false],
		});
		assert.equal((await run()).outcome, "check-failed");
		assert.match(calls.comments[0]!, /after the reviewer's changes/);
		assert.deepEqual(calls.published, []);
	});

	it("doesn't publish when the reviewer leaves uncommitted changes", async () => {
		const { run, calls } = pipeline({
			reviewer: { stdout: '<verdict>{"approved": true, "summary": "Added the null check."}</verdict>', leaveDirty: true },
		});
		assert.equal((await run()).outcome, "check-failed");
		assert.match(calls.comments[0]!, /uncommitted changes/);
		assert.deepEqual(calls.published, []);
	});

	it("fences the reviewer's rejection on the issue", async () => {
		const { run, calls } = pipeline({ reviewer: { stdout: '<verdict>{"approved": false, "summary": "Fixes #3 too."}</verdict>' } });
		await run();
		assert.match(calls.comments[0]!, /```text\nFixes #3 too\.\n```/);
	});

	it("checks the merge with main and publishes the merged commit", async () => {
		const { run, calls, head } = pipeline({ merge: "merged" });
		assert.equal((await run()).outcome, "published");
		assert.equal(calls.checkedAt.at(-1), head());
		assert.equal(calls.published[0]!.sha, head());
	});

	it("doesn't publish when the merge with main fails its check", async () => {
		const { run, calls } = pipeline({ merge: "merged", checks: [true, false] });
		assert.equal((await run()).outcome, "check-failed");
		assert.match(calls.comments[0]!, /after merging main/);
		assert.deepEqual(calls.published, []);
	});

	it("aborts a merge with main that conflicts, and leaves it for a person", async () => {
		const { run, calls } = pipeline({ merge: "conflict" });
		assert.equal((await run()).outcome, "conflicts-with-main");
		assert.ok(calls.aborted);
		assert.match(calls.comments[0]!, /Merge conflict in a\.cs/);
		assert.deepEqual(calls.published, []);
	});

	it("publishes nothing when the branch has no work main lacks", async () => {
		const { run, calls } = pipeline({ ahead: 0 });
		assert.equal((await run()).outcome, "nothing-to-publish");
		assert.deepEqual(calls.runs, ["implementer"]);
		assert.deepEqual(calls.comments, []);
	});

	it("leaves the sandbox running when the git config changed, since closing it runs git on the host", async () => {
		const { run, calls } = pipeline({ configIntact: false });
		await run();
		assert.equal(calls.closed, false);
	});

	it("reports a failed push on the issue", async () => {
		const { run, calls } = pipeline({ publishError: new Error("non-fast-forward") });
		assert.equal((await run()).outcome, "publish-failed");
		assert.match(calls.comments[0]!, /non-fast-forward/);
		assert.ok(calls.closed);
	});
});
