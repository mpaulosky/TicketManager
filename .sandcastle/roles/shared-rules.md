# Shared role rules

## The repo

TicketManager is the .NET solution `TicketManager.slnx`, set up from the Baseline Template. Replace this paragraph with what the repo is for, its projects under `src/` and `tests/`,
and anything every role must know before changing it.

- `CLAUDE.md` says how the repo is built and laid out, `docs/PROCESS.md` how a change reaches `main`, and `docs/adr/` (where present) the decisions behind it.
- `.sandcastle/CODING_STANDARDS.md` holds the code standards every role follows.
- The Baseline Template owns some files: Apply overwrites them, so a change to one here is undone by the next re-Apply. They include `scripts/gate.sh`, the workflows and
  scripts under `.github/`, `.github/hooks/`, `.github/instructions/` and `docs/PROCESS.md`. Don't change them; say in your commit body when an issue needs one changed.

## Build and tests

Use the commands for each stack the issue touches:

| Stack | Where | Build | Run tests |
| --- | --- | --- | --- |
| C# | `src/`, `tests/` | `dotnet build TicketManager.slnx` | `dotnet test <test project>` |
| Sandcastle's TypeScript | `.sandcastle/`, `package.json` | `pnpm exec tsc --noEmit -p .sandcastle` | `pnpm exec tsx --test <file>.test.mts` |
| pnpm workspace | `package.json`, `pnpm-workspace.yaml` | run `pnpm install` after changing `package.json` | commit `pnpm-lock.yaml` in the same commit |

- Tests must pass by the end of the last developer run, not on every commit.
- Lint what you change: `markdownlint-cli2 <file>` for Markdown, `yamllint -c .yamllint.yml <file>` for YAML, `shellcheck <file>` for shell.
- Tests live where the code does: `tests/<Project>.Tests.*` mirrors `src/`, and `.sandcastle/lib/<module>.test.mts` sits beside its module.
- Shell scripts pass `shellcheck`. The code standards are in `.sandcastle/CODING_STANDARDS.md`.

## The gate

In the sandbox, `.sandcastle/sandbox-gate.sh` is the definition of "ready to push". The sandbox has no Docker daemon, so it leaves out the test projects that start containers,
which `scripts/gate.sh` and CI run. It builds the solution with warnings as errors, runs every test project that needs neither Docker nor a browser, and runs Sandcastle's own type check and tests.
It does not lint: lint the Markdown, YAML and shell files you change yourself (see Build and tests above). Run it rather than the individual build and test commands.

- If you committed anything, run `.sandcastle/sandbox-gate.sh` once as your last step, before `<promise>COMPLETE</promise>`, and fix what it reports.
- The tester is exempt, because its tests are red on purpose. It still lints the files it changed.
- The architect is exempt too. It commits at most one ADR, and lints that file with `markdownlint-cli2 <file>`.
- After you finish, the host runs the gate itself. Its exit code decides whether the branch is published, not what you report.
- Leave the worktree clean: commit or discard every change before you finish. Only commits are pushed, so the host fails the gate when anything is left uncommitted.

## Commits

Commit in small steps. Each message follows `.github/instructions/git-commit-instructions.md`:

- Subject: `<type>(<scope>): <Summary>`, imperative, with a capital and no closing period, for example `fix(api): Return 404 for an order that doesn't exist`
- Body: what changed and why, key decisions, and any blockers for the next run
- Footer: `Refs #<issue number>`

## GitHub

Never push, never run `gh`, and never write to GitHub. The sandbox has no GitHub token. The host publishes the branch and does every GitHub write.
