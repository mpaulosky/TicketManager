import assert from "node:assert/strict";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { describe, it } from "node:test";
import { GitConfigGuard, gitConfigTampered } from "./shell.mts";

describe("GitConfigGuard", () => {
	const withConfig = (test: (config: string, worktreeConfig: string) => void) => {
		const dir = mkdtempSync(join(tmpdir(), "sandcastle-guard-"));
		try {
			const config = join(dir, "config");
			writeFileSync(config, '[remote "origin"]\n\turl = https://github.com/o/r.git\n');
			test(config, join(dir, "config.worktree"));
		} finally {
			rmSync(dir, { recursive: true, force: true });
		}
	};

	it("passes while the config is unchanged", () => {
		withConfig((config, worktreeConfig) => {
			new GitConfigGuard([config, worktreeConfig]).verify();
		});
	});

	it("refuses once a config file changes or appears", () => {
		withConfig((config, worktreeConfig) => {
			const guard = new GitConfigGuard([config, worktreeConfig]);
			writeFileSync(worktreeConfig, "[credential]\n\thelper = !touch /tmp/pwned\n");
			assert.throws(() => guard.verify(), /Git config changed/);
			assert.ok(gitConfigTampered());
		});
	});
});
