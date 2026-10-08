# Coding Standards

The reviewer loads this file during code review. It sums up the rules this repository already enforces; the sources
below are authoritative, so read the one that applies when a rule here isn't specific enough, and when they disagree
follow the source.

- [docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md): stack, solution layout, testing and design rules
- [.github/copilot-instructions.md](../.github/copilot-instructions.md): working agreements, C#, testing and validation
- [.editorconfig](../.editorconfig): formatting and analyzer settings
- [docs/PROCESS.md](../docs/PROCESS.md) and
  [.github/instructions/git-commit-instructions.md](../.github/instructions/git-commit-instructions.md): branches and
  commits
- [.github/instructions/markdown.instructions.md](../.github/instructions/markdown.instructions.md): Markdown

## Style

- Follow `.editorconfig`: tabs (width 2) for C#, Razor, TypeScript, JSON, HTML, CSS and XML; two spaces for Markdown
  and YAML; LF line endings, UTF-8, a final newline and no trailing whitespace.
- Target .NET 10 with the latest C# version. Nullable reference types, analyzers, code style enforcement and
  warnings-as-errors are on, so the build fails on any new warning: fix the warning, don't suppress it.
- Document public APIs and class members with XML `/// <summary>` comments.
- Avoid empty catch blocks and `Thread.Sleep` in production code. Handle exceptions deliberately: log meaningful
  context and rethrow or handle the error.
- Never add secrets, credentials, connection strings or tokens to source, configuration, tests or docs. Use
  configuration and environment variables.
- Flag user input that is used without validation or sanitization.
- Use pnpm, never npm or npx, for Node tooling.

## Testing

- xUnit v3 for unit and integration tests, bUnit for Blazor components, Playwright for end-to-end tests, with
  FluentAssertions and NSubstitute where they fit. Follow the conventions of the nearby tests.
- New behaviour and bug fixes come with tests, written test-first (red-green-refactor) where practical.
- Test methods keep the `// Arrange`, `// Act` and `// Assert` markers.
- Never change the code under test just to make a test pass: fix the implementation, or fix the test to state the
  intended behaviour.
- `tests/` mirrors `src/`. A change must pass `scripts/gate.sh`, which lints, builds the solution and runs every test
  project.

## Architecture

- `src/` holds the AppHost, ServiceDefaults and the Blazor Web project; .NET Aspire composes them.
- GitHub is the only data store: no database and no persisted copy of issue data. Every read and write of issues and
  projects goes through the GitHub client abstraction, never ad hoc calls from UI code, so auth, rate limiting and
  pagination stay in one place.
- Package versions live in `Directory.Packages.props` (Central Package Management); project files reference packages
  without versions. Make the smallest justified package change.
- The UI uses Tailwind CSS v4, built from `src/Web/wwwroot/css/app.tailwind.css`. Reuse existing components and
  patterns, and cover loading, empty, validation, error and authorization states.
- Keep changes focused on the issue, prefer existing abstractions over new infrastructure, and update the docs when
  behaviour, setup or validation changes.
