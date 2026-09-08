# TicketManager — Architecture & Project Guidelines

## Purpose

TicketManager is a tool for managing GitHub Issues through GitHub Projects. It lets a
user switch between GitHub Projects and manage that project's issues (create, update,
triage, view) without leaving the app. GitHub itself is the system of record — there is
no local database or persisted copy of issue data.

## Technology stack

- **.NET 10** targeting the latest C# language version.
- **.NET Aspire** (latest) for orchestration, service defaults, and local dev
  composition (AppHost + ServiceDefaults projects).
- **Central Package Management** (`Directory.Packages.props`) — all NuGet package
  versions are pinned centrally; project files reference packages without versions.
- **Blazor** for the UI, styled with **Tailwind CSS**.
- **GitHub** (via the GitHub API/GraphQL and GitHub Projects) as the sole backing
  store for issues and project data. No SQL/NoSQL database is used.

## Solution layout

- `src/` — application projects (AppHost, ServiceDefaults, Web/Blazor UI, and any
  supporting class libraries for GitHub integration).
- `tests/` — test projects, mirroring the structure of `src/`.
- `TicketManager.slnx` — solution file referencing all projects.

## Testing

- **xUnit v3** for unit and integration tests.
- **bUnit** for Blazor component tests.
- **Playwright** for end-to-end tests against the running Blazor UI.
- Prefer test-first (red-green-refactor) for new behavior and bug fixes.
- Follow repository test-method conventions (Arrange/Act/Assert markers).

## Working agreements

- Since GitHub is the data source, all reads/writes to issues and projects go through
  a well-defined GitHub client abstraction rather than being called ad hoc from UI
  code — this keeps auth, rate-limiting, and pagination concerns centralized.
- Keep changes focused and follow existing project conventions in `.editorconfig`,
  `Directory.Build.props`, and `Directory.Packages.props` once they exist.
- Never commit GitHub tokens or credentials; use configuration/environment variables
  or user secrets.
