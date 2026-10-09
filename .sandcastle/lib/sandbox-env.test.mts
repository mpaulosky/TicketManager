import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { githubTokensIn } from "./sandbox-env.mts";

describe("githubTokensIn", () => {
	it("finds GH_TOKEN and GITHUB_TOKEN, even when blank or exported", () => {
		const env = "CLAUDE_CODE_OAUTH_TOKEN=abc\nGH_TOKEN=\nexport GITHUB_TOKEN=xyz\n";
		assert.deepEqual(githubTokensIn(env), ["GH_TOKEN", "GITHUB_TOKEN"]);
	});

	it("ignores comments and other keys", () => {
		const env = "# GH_TOKEN=old\nANTHROPIC_API_KEY=k\nNOT_GH_TOKEN=1\n";
		assert.deepEqual(githubTokensIn(env), []);
	});
});
