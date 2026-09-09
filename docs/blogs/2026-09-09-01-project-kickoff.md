---
post_title: "Bootstrapping TicketManager: Engineering Practices Before a Line of App Code"
author1: "mpaulosky"
post_slug: "project-kickoff"
microsoft_alias: "mpaulosky"
featured_image: ""
categories: ["Engineering", "Tooling", "Documentation"]
tags: ["bootstrap", "ci-cd", "tdd", "vertical-slice", "git-hooks", "copilot"]
ai_note: "This post was drafted with AI assistance (GitHub Copilot / Squad agent)
based on the actual repository history."
summary: "Before any application code existed, the TicketManager repository
landed eleven commits that established session tracking, agent
configuration, git hooks, documentation standards, CI/CD workflows, and
architecture guidelines — the scaffolding every later feature would build on."
post_date: "2026-09-09"
---

## Setting the stage before the first feature

Most projects start with a `Hello World` controller or a stub domain model.
TicketManager started differently: the first eleven commits on `main`, all
landing on 2026-09-08, contain zero application source under `src/`. Instead
they lay down the process, automation, and documentation scaffolding that
every subsequent pull request — including the app itself — would depend on.
Reading through them in order (`git log --oneline --reverse`) tells a clear
story of a repo bootstrapping its own engineering discipline first.

## Session and agent tracking from commit one

The very first commit, `4d7eb45`, adds a single line to
`.claude/scheduled_tasks.lock` — a lock file used to track session
information for scheduled agent tasks. It is a small file, but it signals
the intent from the start: this repository expects AI agents to operate on
it in a tracked, auditable way, not as an afterthought bolted on later.

The next commit, `2b3fdc7`, follows up with a 192-line
`.github/agents/beast.agent.md`, configuring a "Beast" agent backed by
GPT-4.1 with an extensive toolset and behavioral guidelines. Together these
two commits establish that agent-assisted work is a first-class citizen of
the repository, with its own configuration surface and session bookkeeping.

## Guardrails before automation: git hooks and content standards

Commit `ed541be` adds three git hook scripts under `.github/hooks/`:
`pre-commit`, `pre-push`, and `post-checkout`. These hooks give the
repository local, fast-feedback validation gates that run before code ever
reaches CI — catching problems on a developer's or agent's machine instead
of in a pull request.

Immediately after, `a238e36` codifies the rules those hooks (and humans)
should follow: seven new files under `.github/instructions/`, covering git
commit message conventions, markdown content standards, Blazor component
patterns, and dotnet project conventions, plus a repository-wide Copilot
instructions file. This is the commit that produced the very
`markdown.instructions.md` rules this post is written to follow — front
matter fields, heading levels, and line-length guidance all trace back to
this single commit.

## Structured prompts for planning and implementation

With hooks and standards in place, `721a6da` adds fifteen prompt files under
`.github/prompts/`, including `structured-autonomy-plan.prompt.md`,
`structured-autonomy-implement.prompt.md`, and topic-specific prompts for
xUnit testing, C# documentation, editor configuration, and README
generation. These prompts give both humans and agents a repeatable,
structured way to move from an idea to a plan to a merged change, rather
than improvising the workflow each time.

## Testing philosophy and architecture, written down early

`f5d2494` is the largest of the early commits by file count: twenty-three
files under `.github/skills/`, including a full `tdd/` skill (deep modules,
interface design, mocking, refactoring, and test-writing guidance) and a
1,046-line `vertical-slice/` skill that scaffolds CQRS, MediatR, MongoDB
repositories, and a complete Blazor UI for a feature slice. This commit
effectively pre-writes the architectural pattern the application itself
would later be built with — vertical slices, not layered services — before
any slice existed.

## Automating the pipeline

Commit `b97ffe8` introduces ten GitHub Actions workflows: a 517-line
`ci.yml`, a `code-metrics.yml` workflow, `codeql-analysis.yml` for security
scanning, `dependabot-auto-merge.yml`, `label-enforce.yml`,
`pr-automerge.yml`, a 557-line `release.yml`, markdown and YAML linting
workflows, and `sync-readme.yml` for keeping generated documentation
current. This single commit gives the repository code metrics, static
analysis, and documentation synchronization on every push, well before
there was any application code for those workflows to analyze.

Right behind it, `13d687e` adds `.github/copilot-instructions.md`,
`.github/dependabot.yml`, and `.github/mcp-config.json` — wiring up
Copilot's repository-level instructions, automated dependency updates via
Dependabot, and MCP server configuration for tool-assisted workflows.

## Community and policy documents

`0aedbe9` adds four community-health documents in one commit:
`docs/CODE_OF_CONDUCT.md`, `docs/CONTRIBUTING.md`, `docs/REFERENCES.md`, and
`docs/SECURITY.md`. These establish expectations for contributor behavior,
the contribution workflow, reference material, and how to report security
issues — all before there was a public surface area to secure.

## Editor, package, and version control configuration

`657861a` bundles ten configuration files: `.editorconfig` (448 lines,
covering both C# and markdown formatting), `.gitignore`, markdown and YAML
lint configs (`.markdownlint-cli2.jsonc`, `.markdownlint.json`,
`.yamllint.yml`), `GitVersion.yml` for semantic versioning, the `LICENSE`
file, `NuGet.config`, the `TicketManager.slnx` solution file, and
`global.json` pinning the .NET SDK version. This is the commit that makes
the repository buildable and consistently formatted across contributors and
editors.

## Architecture and terminology, defined last

The final two commits round out the documentation base. `7331fc1` adds
`docs/ARCHITECTURE.md`, describing the repository's layout and policy
boundaries. `1f026d7` adds `CONTEXT.md` at the repository root, documenting
TicketManager's own terminology and workflow — the domain language ("Ticket",
"author", "administrator") that the eventual application would be built
around.

## Why this order matters

None of these eleven commits touch `src/` or `tests/`. Taken individually,
each looks like routine repository housekeeping. Read together and in
order, they show a deliberate sequence: track agent sessions and configure
agents first, add validation hooks, write down the standards those hooks
enforce, script the planning workflow, document the testing and
architectural philosophy, automate CI/CD, add community policies, pin down
editor and package configuration, and finally describe the architecture and
domain. By the time the first feature-bearing pull request (`#1`) landed,
every gate a future change would need to pass — commit message format,
markdown linting, CI, code metrics, security scanning — was already in
place and enforced.
