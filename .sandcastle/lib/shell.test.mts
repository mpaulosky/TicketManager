import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { describe, it } from "node:test";
import { GitConfigGuard, gitConfigIntact, gitConfigTampered, trustGitConfig, worktreeIntact } from "./shell.mts";

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

describe("trustGitConfig", () => {
	it("notices a commondir file that would point git at another config", () => {
		const dir = mkdtempSync(join(tmpdir(), "sandcastle-repo-"));
		try {
			execFileSync("git", ["init", "--quiet", dir]);
			trustGitConfig(dir);
			assert.ok(gitConfigIntact());
			mkdirSync(join(dir, ".git", "fake"));
			writeFileSync(join(dir, ".git", "commondir"), "fake\n");
			assert.equal(gitConfigIntact(), false);
		} finally {
			rmSync(dir, { recursive: true, force: true });
		}
	});
});

describe("worktreeIntact", () => {
	const withWorktree = (test: (worktree: string, common: string, gitDir: string) => void) => {
		const dir = mkdtempSync(join(tmpdir(), "sandcastle-wt-"));
		try {
			const common = join(dir, "repo", ".git");
			const gitDir = join(common, "worktrees", "wt");
			const worktree = join(dir, "wt");
			mkdirSync(gitDir, { recursive: true });
			mkdirSync(worktree);
			writeFileSync(join(worktree, ".git"), `gitdir: ${gitDir}\n`);
			writeFileSync(join(gitDir, "commondir"), "../..\n");
			test(worktree, common, gitDir);
		} finally {
			rmSync(dir, { recursive: true, force: true });
		}
	};

	it("accepts the files git wrote", () => {
		withWorktree((worktree, common) => assert.ok(worktreeIntact(worktree, common)));
	});

	it("rejects a .git file pointing elsewhere", () => {
		withWorktree((worktree, common) => {
			writeFileSync(join(worktree, ".git"), "gitdir: /tmp/evil\n");
			assert.equal(worktreeIntact(worktree, common), false);
		});
	});

	it("rejects a changed commondir or an added config.worktree", () => {
		withWorktree((worktree, common, gitDir) => {
			writeFileSync(join(gitDir, "commondir"), "/tmp/evil\n");
			assert.equal(worktreeIntact(worktree, common), false);
		});
		withWorktree((worktree, common, gitDir) => {
			writeFileSync(join(gitDir, "config.worktree"), "[core]\n\tfsmonitor = touch /tmp/pwned\n");
			assert.equal(worktreeIntact(worktree, common), false);
		});
	});
});
