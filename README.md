# TicketManager

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
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
[ci.yml](.github/workflows/ci.yml); the workflow currently emits a warning when
coverage falls below that threshold rather than failing the build, so it is a
target, not a hard-enforced gate.*

## Purpose

TicketManager is a tool for managing GitHub Issues through GitHub Projects. It lets a
user switch between GitHub Projects and manage that project's issues (create, update,
triage, view) without leaving the app. GitHub itself is the system of record — there
is no local database or persisted copy of issue data. See
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full architecture and technology
stack.

**Current implementation state:** this repository is an early-stage .NET Aspire /
Blazor application. [src/Web](src/Web) includes a Home overview page, a GitHub
Projects dashboard page, and an About page, styled with Tailwind CSS — GitHub Issues
create/update/triage workflows are not yet implemented.

## Repository structure

- [.github/workflows](.github/workflows) — canonical CI/CD, lint, triage, sync,
  and release automation workflows.
- [.github/hooks](.github/hooks) — local hook scripts (including pre-push gates).
- [.github/instructions](.github/instructions) — coding and documentation
  instructions applied to contributors and agents.
- [src/AppHost](src/AppHost) — .NET Aspire orchestration project that composes and
  launches the app's services for local development.
- [src/ServiceDefaults](src/ServiceDefaults) — shared Aspire service-defaults
  (telemetry, health checks, resilience) referenced by other src projects.
- [src/Web](src/Web) — the Blazor UI project (Home, GitHub Repositories dashboard, and
  About pages, styled with Tailwind CSS).
- [tests/AppHost.Tests](tests/AppHost.Tests) — integration tests for the AppHost
  orchestration project.
- [tests/Architecture.Tests](tests/Architecture.Tests) — architecture/convention tests
  enforcing project boundaries.
- [tests/Web.Tests.Unit](tests/Web.Tests.Unit) — unit tests for the Web project.
- [tests/Web.Tests.Bunit](tests/Web.Tests.Bunit) — bUnit component tests for Blazor
  components in Web.
- [tests/Web.Tests.Integration](tests/Web.Tests.Integration) — integration tests for
  the Web project.
- [tests/Web.Tests.E2E](tests/Web.Tests.E2E) — Playwright end-to-end tests against the
  running Blazor UI.
- All of the above are wired into [TicketManager.slnx](TicketManager.slnx).
- [docs](docs) — architecture, contribution guidance, and release-review history.
- [Directory.Packages.props](Directory.Packages.props), [global.json](global.json),
  [GitVersion.yml](GitVersion.yml), and [NuGet.config](NuGet.config) — shared
  dependency/version governance; `NuGet.config` restricts restores to nuget.org as
  the sole package source, with package source mapping.

## Documentation index

- [Docs landing page](docs/index.html) — overview and documentation entry points.
- [Architecture overview](docs/ARCHITECTURE.md) — repository layout and policy
  boundaries.
- [Contributing guide](docs/CONTRIBUTING.md) — contribution workflow and validation
  expectations.
- [Release review blog index](docs/blogs/README.md) — release-review post index.

## Release review blogs

The release review posts in [docs/blogs](docs/blogs/README.md) summarize the
rollout history of workflow-standard and major changes by release.

### Latest blogs (top 5, generated)

<!-- BLOG_START -->
| Date | Title | Tags |
|------|-------|------|
| 2026-09-12 | [chore(tests): adopt FluentAssertions and close coverage gaps](docs/blogs/2026-09-12-pr-42-chore-tests-adopt-fluentassertions-and-close-coverage-gaps.md) | release,automation |
| 2026-09-12 | [refactor(web): rename GitHub Projects dashboard to GitHub Repositories](docs/blogs/2026-09-12-pr-40-refactor-web-rename-github-projects-dashboard-to-github-repositories.md) | release,automation |
| 2026-09-12 | [refactor(web): deepen GitHubMetadataProvider with an injectable git seam](docs/blogs/2026-09-12-pr-38-refactor-web-deepen-githubmetadataprovider-with-an-injectable-git-seam.md) | release,automation |
| 2026-09-12 | [refactor(web): extract IGitHubRestClient to deduplicate GitHub API access](docs/blogs/2026-09-12-pr-36-refactor-web-extract-igithubrestclient-to-deduplicate-github-api-access.md) | release,automation |
| 2026-09-11 | [feat(web): add About page](docs/blogs/2026-09-11-pr-34-feat-web-add-about-page.md) | release,automation |
<!-- BLOG_END -->

## Quick start

### For adopters (using this standard in another repo)

1. Review the standard and policy surface in
   [.github/workflows](.github/workflows), [.github/hooks](.github/hooks), and
   [.github/instructions](.github/instructions).
2. Copy the assets you want to adopt into your target repository.
3. Validate in CI and locally with the same gates this repo uses
   (workflows in `.github/workflows`, hook behavior in `.github/hooks/pre-push`).
4. Track updates through releases and release-review posts in
   [docs/blogs](docs/blogs/README.md).

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
   [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md).

## Merged-PR release automation (concise)

The [Release workflow](.github/workflows/release.yml) runs when a PR
is merged to `main` (or by manual dispatch with a merged PR number).

Every merged PR to `main` is treated as release-eligible. The workflow runs one
release/blog pass per PR (idempotency marker: `Source PR: #<number>` in release
notes) and then:

- computes semver bump from PR labels, with idempotency checks to avoid duplicate
  releases,
- generates a release-review post and rebuilds
  [docs/blogs/README.md](docs/blogs/README.md),
- updates this README latest-blog block (`<!-- BLOG_START -->
| Date | Title | Tags |
|------|-------|------|
| 2026-09-12 | [chore(tests): adopt FluentAssertions and close coverage gaps](docs/blogs/2026-09-12-pr-42-chore-tests-adopt-fluentassertions-and-close-coverage-gaps.md) | release,automation |
| 2026-09-12 | [refactor(web): rename GitHub Projects dashboard to GitHub Repositories](docs/blogs/2026-09-12-pr-40-refactor-web-rename-github-projects-dashboard-to-github-repositories.md) | release,automation |
| 2026-09-12 | [refactor(web): deepen GitHubMetadataProvider with an injectable git seam](docs/blogs/2026-09-12-pr-38-refactor-web-deepen-githubmetadataprovider-with-an-injectable-git-seam.md) | release,automation |
| 2026-09-12 | [refactor(web): extract IGitHubRestClient to deduplicate GitHub API access](docs/blogs/2026-09-12-pr-36-refactor-web-extract-igithubrestclient-to-deduplicate-github-api-access.md) | release,automation |
| 2026-09-11 | [feat(web): add About page](docs/blogs/2026-09-11-pr-34-feat-web-add-about-page.md) | release,automation |
<!-- BLOG_END -->`)
  from `docs/blogs/README.md` (top 5 rows),
- updates [docs/index.html](docs/index.html) latest blog links from the same rows.
