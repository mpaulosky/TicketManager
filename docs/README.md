# TicketManager

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](../LICENSE)
[![xUnit Tests](https://img.shields.io/badge/Tests-xUnit-blueviolet?logo=github)](https://github.com/mpaulosky/TicketManager/actions/workflows/ci.yml)
[![Latest Release](https://img.shields.io/github/v/release/mpaulosky/TicketManager?logo=github&color=blue&label=Release)](https://github.com/mpaulosky/TicketManager/releases/latest)

[![CI/CD](https://github.com/mpaulosky/TicketManager/actions/workflows/ci.yml/badge.svg)](https://github.com/mpaulosky/TicketManager/actions/workflows/ci.yml)
[![CodeCov Coverage](https://codecov.io/gh/mpaulosky/TicketManager/branch/main/graph/badge.svg)](https://codecov.io/gh/mpaulosky/TicketManager)
[![Coverage Target](https://img.shields.io/badge/Coverage%20Target-≥80%25-brightgreen?logo=codecov)](https://github.com/mpaulosky/TicketManager/actions/workflows/ci.yml)

[![Open Issues](https://img.shields.io/github/issues/mpaulosky/TicketManager?color=0366d6)](https://github.com/mpaulosky/TicketManager/issues?q=is%3Aopen+is%3Aissue)
[![Closed Issues](https://img.shields.io/github/issues-closed/mpaulosky/TicketManager?color=6f42c1)](https://github.com/mpaulosky/TicketManager/issues?q=is%3Aclosed+is%3Aissue)
[![Open PRs](https://img.shields.io/github/issues-pr/mpaulosky/TicketManager?color=28a745)](https://github.com/mpaulosky/TicketManager/pulls?q=is%3Aopen+is%3Apr)
[![Closed PRs](https://img.shields.io/github/issues-pr-closed/mpaulosky/TicketManager?color=6f42c1)](https://github.com/mpaulosky/TicketManager/pulls?q=is%3Aclosed+is%3Apr)

*Note: the "Coverage Target" badge above reflects the 80% goal checked in
[ci.yml](../.github/workflows/ci.yml); the workflow currently emits a warning when
coverage falls below that threshold rather than failing the build, so it is a
target, not a hard-enforced gate.*

## Purpose

TicketManager is a tool for managing GitHub Issues through GitHub Projects. It lets a
user switch between GitHub Projects and manage that project's issues (create, update,
triage, view) without leaving the app. GitHub itself is the system of record — there
is no local database or persisted copy of issue data. See
[docs/ARCHITECTURE.md](ARCHITECTURE.md) for the full architecture and technology
stack.

**Current implementation state:** this repository is an early-stage .NET Aspire /
Blazor application. [src/Web](../src/Web) includes a Home overview page, a GitHub
Projects dashboard page, and an About page, styled with Tailwind CSS — GitHub Issues
create/update/triage workflows are not yet implemented.

## Repository structure

- [.github/workflows](../.github/workflows) — canonical CI/CD, lint, triage, sync,
  and release automation workflows.
- [.github/hooks](../.github/hooks) — local hook scripts (including pre-push gates).
- [.github/instructions](../.github/instructions) — coding and documentation
  instructions applied to contributors and agents.
- [src/AppHost](../src/AppHost) — .NET Aspire orchestration project that composes and
  launches the app's services for local development.
- [src/ServiceDefaults](../src/ServiceDefaults) — shared Aspire service-defaults
  (telemetry, health checks, resilience) referenced by other src projects.
- [src/Web](../src/Web) — the Blazor UI project (Home, GitHub Repositories dashboard, and
  About pages, styled with Tailwind CSS).
- [tests/AppHost.Tests](../tests/AppHost.Tests) — integration tests for the AppHost
  orchestration project.
- [tests/Architecture.Tests](../tests/Architecture.Tests) — architecture/convention tests
  enforcing project boundaries.
- [tests/Web.Tests.Unit](../tests/Web.Tests.Unit) — unit tests for the Web project.
- [tests/Web.Tests.Bunit](../tests/Web.Tests.Bunit) — bUnit component tests for Blazor
  components in Web.
- [tests/Web.Tests.Integration](../tests/Web.Tests.Integration) — integration tests for
  the Web project.
- [tests/Web.Tests.E2E](../tests/Web.Tests.E2E) — Playwright end-to-end tests against the
  running Blazor UI.
- All of the above are wired into [TicketManager.slnx](../TicketManager.slnx).
- [docs](./) — architecture, contribution guidance, and release-review history.
- [Directory.Packages.props](../Directory.Packages.props), [global.json](../global.json),
  [GitVersion.yml](../GitVersion.yml), and [NuGet.config](../NuGet.config) — shared
  dependency/version governance; `NuGet.config` restricts restores to nuget.org as
  the sole package source, with package source mapping.

## Documentation index

- [Docs landing page](index.html) — overview and documentation entry points.
- [Architecture overview](ARCHITECTURE.md) — repository layout and policy
  boundaries.
- [Contributing guide](CONTRIBUTING.md) — contribution workflow and validation
  expectations.
- [Release review blog index](blogs/README.md) — release-review post index.

## Release review blogs

The release review posts in [docs/blogs](blogs/README.md) summarize the
rollout history of workflow-standard and major changes by release.

### Latest blogs (top 5, generated)

## Quick start

### For adopters (using this standard in another repo)

1. Review the standard and policy surface in
   [.github/workflows](../.github/workflows), [.github/hooks](../.github/hooks), and
   [.github/instructions](../.github/instructions).
2. Copy the assets you want to adopt into your target repository.
3. Validate in CI and locally with the same gates this repo uses
   (workflows in `.github/workflows`, hook behavior in `.github/hooks/pre-push`).
4. Track updates through releases and release-review posts in
   [docs/blogs](blogs/README.md).

### For contributors (updating this repo)

1. Clone and restore:

   ```bash
   git clone https://github.com/mpaulosky/TicketManager.git
   cd TicketManager
   dotnet restore TicketManager.slnx
   ```

2. Run baseline validation:

   ```bash
   dotnet build TicketManager.slnx --configuration Release
   dotnet test TicketManager.slnx
   ```

   > `tests/Web.Tests.E2E` requires the Playwright browser binaries to be installed
   > first, or its tests will fail. After building, install them with:
   > `pwsh tests/Web.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install`
   > (adjust the path if you built a different configuration).

3. Follow the full contribution workflow in
   [docs/CONTRIBUTING.md](CONTRIBUTING.md).

## Merged-PR release automation (concise)

The [Release workflow](../.github/workflows/release.yml) runs when a PR
is merged to `main` (or by manual dispatch with a merged PR number).

Every merged PR to `main` is treated as release-eligible. The workflow runs one
release/blog pass per PR (idempotency marker: `Source PR: #<number>` in release
notes) and then:

- computes semver bump from PR labels, with idempotency checks to avoid duplicate
  releases,
- generates a release-review post and rebuilds
  [docs/blogs/README.md](blogs/README.md),
- updates this README latest-blog block (``)
  from `docs/blogs/README.md` (top 5 rows),

- updates [docs/index.html](index.html) latest blog links from the same rows.

## Releases

<!-- RELEASES_START -->

| Version | Date | Title | Blog post |
| ------- | ---- | ----- | --------- |
| [v0.0.79](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.79) | 2026-10-09 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-09-pr-175-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.0.78](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.78) | 2026-10-09 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-09-pr-173-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.0.77](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.77) | 2026-10-09 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-09-pr-171-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.0.76](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.76) | 2026-10-09 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-09-pr-169-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.0.75](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.75) | 2026-10-09 | chore: Re-apply the repo-ci-baseline Template | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-09-pr-167-chore-re-apply-the-repo-ci-baseline-template.md) |
| [v0.0.74](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.74) | 2026-10-09 | fix(sandcastle): Publish each issue as its own PR, from a Docker-free check | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-09-pr-159-fix-sandcastle-publish-each-issue-as-its-own-pr-from-a-docker-free-check.md) |
| [v0.0.73](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.73) | 2026-10-09 | ci: Run the Sandcastle tests when gate.sh or ci.yml changes | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-09-pr-162-ci-run-the-sandcastle-tests-when-gate-sh-or-ci-yml-changes.md) |
| [v0.0.72](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.72) | 2026-10-09 | ci: Type-check and run the Sandcastle tests in the gate and in CI | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-09-pr-158-ci-type-check-and-run-the-sandcastle-tests-in-the-gate-and-in-ci.md) |
| [v0.0.71](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.71) | 2026-10-08 | chore(sandcastle): Have agents write Conventional Commits | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-08-pr-153-chore-sandcastle-have-agents-write-conventional-commits.md) |
| [v0.0.70](https://github.com/mpaulosky/TicketManager/releases/tag/v0.0.70) | 2026-10-08 | chore(claude): Stop tracking .claude/scheduled_tasks.lock | [Post](https://github.com/mpaulosky/TicketManager/blob/main/docs/blogs/2026-10-08-pr-156-chore-claude-stop-tracking-claude-scheduled-tasks-lock.md) |

<!-- RELEASES_END -->

[All releases →](https://github.com/mpaulosky/TicketManager/releases)
