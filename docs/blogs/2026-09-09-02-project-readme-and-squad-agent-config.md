---
post_title: "PR #1: A README, Central Package Management, and the Squad Agent"
author1: "mpaulosky"
post_slug: "project-readme-and-squad-agent-config"
microsoft_alias: "mpaulosky"
featured_image: ""
categories: ["Engineering", "Tooling"]
tags: ["central-package-management", "nuget", "squad", "readme", "github-actions"]
ai_note: "This post was drafted with AI assistance (GitHub Copilot / Squad agent)
based on the actual repository history."
summary: "Pull request #1 gave TicketManager its public-facing README,
centralized NuGet version management via Directory.Packages.props, and the
1,127-line Squad agent configuration that orchestrates the rest of the AI
team living in the repo."
post_date: "2026-09-09"
---

## The first pull request

After eleven ungrouped commits laid down process and tooling (see the
companion post on the project kickoff), commit `51812bf`, "Add project
README, central package management, and Squad agent config," landed as the
repository's first actual pull request, `#1`. It touched four files and
added 1,313 lines: `.github/agents/squad.agent.md`, `.gitignore`,
`Directory.Packages.props`, and `README.md`.

## Central package management with Directory.Packages.props

The new `Directory.Packages.props` file at the repository root turns on
`ManagePackageVersionsCentrally`, which moves every NuGet package version
out of individual `.csproj` files and into one shared manifest. Project
files then reference packages by name only (`<PackageReference
Include="FluentAssertions" />`), while this single file owns the version
number (`<PackageVersion Include="FluentAssertions" Version="8.10.0" />`).

This matters for a multi-project solution like TicketManager for a few
concrete reasons visible in the file itself:

- **One version, everywhere.** With dozens of packages spanning Aspire
  hosting, Microsoft.Extensions.*, OpenTelemetry, and the test stack
  (bunit, FluentAssertions, NSubstitute, Microsoft.Playwright, xunit.v3),
  a single project accidentally pinning a different version of, say,
  `Microsoft.Extensions.Configuration` than another project is a common
  source of runtime binding errors. Central package management makes that
  class of bug structurally impossible.
- **Deterministic, CI-aware builds.** The file also sets
  `Deterministic=true` and conditionally enables
  `ContinuousIntegrationBuild` when the `CI` environment variable is set,
  which improves build reproducibility on GitHub Actions runners.
- **Explicit, documented suppressions.** Rather than silently ignoring
  warnings, the file uses `WarningsNotAsErrors` for `NU1603`/`NU1608`
  (package version resolution warnings) and `NoWarn` for `NU1902`/`NU1903`
  (security advisories), with an inline comment noting these are
  specifically for transitive dependencies pulled in by `MongoDB.Driver
  3.8.0`. That makes the exception traceable instead of mysterious.
- **One place to see the whole dependency surface.** The file organizes
  packages into commented groups — Aspire, Core Libraries, Microsoft
  Extensions, OpenTelemetry, and Testing Packages — so anyone reviewing a
  diff to this file can see the entire package surface of the solution at
  a glance, without opening every project file.

## A public front door: README.md

The commit also adds the repository's first `README.md`. It leads with
status badges (.NET 10, MIT license, xUnit tests, latest release, CI/CD,
CodeCov coverage, a coverage gate badge, and open/closed issue and PR
counts), then documents:

- **Purpose** — a description of TicketManager as a web application for
  authoring, categorizing, and publishing tickets, with administrator
  oversight of content quality and compliance.
- **Repository structure** — links to `.github/workflows`,
  `.github/hooks`, `.github/instructions`, `src`, `tests`, `docs`, and the
  shared dependency/version files (`Directory.Packages.props`,
  `global.json`, `GitVersion.yml`).
- **Documentation index** — links to `docs/index.html`,
  `docs/ARCHITECTURE.md`, `docs/CONTRIBUTING.md`, and a release-review
  blog index at `docs/blogs/README.md`.
- **A generated "latest blogs" table** — bounded by
  `<!-- BLOG_START -->`/`<!-- BLOG_END -->` HTML comment markers, which the
  README explains is kept in sync by the `Squad Release workflow` whenever
  a pull request merges to `main`.
- **Quick start instructions** for two audiences: adopters who want to copy
  the workflow-standard assets into another repository, and contributors
  who want to clone, restore, build, and test this one.
- **Merged-PR release automation**, describing how every merged PR to
  `main` triggers one release/blog pass, guarded by an idempotency marker
  (`Source PR: #<number>`) in the release notes to avoid duplicate
  releases.

## The Squad agent: an AI team that lives in the repo

The largest addition in the commit is `.github/agents/squad.agent.md`, at
1,127 lines. It defines "Squad (Coordinator)," an orchestration agent whose
job is to assemble and manage a team of specialist agents for the
repository rather than to produce domain artifacts itself. Its own
refusal rules, stated directly in the file, are notable:

- It may **not** generate domain artifacts (code, designs, analyses)
  itself — it must spawn a specialist agent for that.
- It may **not** bypass reviewer approval on rejected work.
- It may **not** invent facts or assumptions — it must ask the user or
  spawn an agent that knows.
- It may **not** do the work itself; it must always delegate, with a
  narrow exception for "Direct Mode" (status checks and simple factual
  answers).

The file also documents a state-resolution protocol: before deciding
whether it is in "Init Mode" (no team configured yet) or "Team Mode"
(a roster already exists), the coordinator checks `.squad/config.json` for
an external or remote team-state location, falling back to a local
`.squad/team.md`. This is what the accompanying one-line change to
`.gitignore` — adding `.squad/` — supports: the Squad framework's working
state is treated as local, generated tooling state, not something checked
into version control alongside the team roster definition itself.

## Why this pairing makes sense as one PR

At first glance, a README, a package-management manifest, and an AI-agent
configuration file look like three unrelated changes. In context, they are
the same milestone: this is the commit where TicketManager became not just
a set of standards but a project with a name, a public description, a
managed dependency graph, and an orchestrator capable of assembling the
rest of the team that would build on top of the scaffolding from the prior
eleven commits.
