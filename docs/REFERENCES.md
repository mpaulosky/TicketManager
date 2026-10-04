# References Used In TicketManager

## Technologies & Frameworks

- [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) – Main platform
- [C# 14.0](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14) – Latest C# language features
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) – Orchestration and service defaults (`src/AppHost`, `src/ServiceDefaults`)
- [Blazor Web App](https://learn.microsoft.com/aspnet/core/blazor/) – Interactive server-side rendering UI framework
- [Tailwind CSS](https://tailwindcss.com/) – Styling, built with the Tailwind CLI through pnpm (`src/Web/package.json`)
- [GitHub REST API](https://docs.github.com/rest) – The system of record for projects and issues
- [Auth0](https://auth0.com/docs) – Optional sign-in
- [OpenTelemetry](https://opentelemetry.io/docs/languages/dotnet/) – Tracing and metrics via the Aspire service defaults

## Testing Tools

- [xUnit v3](https://xunit.net/) on [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) – Test framework and runner (`tests/`)
- [FluentAssertions](https://fluentassertions.com/) – Fluent assertion library for all tests
- [bUnit](https://bunit.dev/) – Blazor component tests (`tests/Web.Tests.Bunit`)
- [Playwright](https://playwright.dev/dotnet/) – End-to-end browser tests (`tests/Web.Tests.E2E`)
- [NetArchTest.Rules](https://github.com/BenMorris/NetArchTest) – Architecture testing (`tests/Architecture.Tests`)
- [Aspire.Hosting.Testing](https://learn.microsoft.com/dotnet/aspire/testing/write-your-first-test) – AppHost tests (`tests/AppHost.Tests`)
- [Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/aspnet/core/test/integration-tests) – Integration testing support
- [Microsoft.Testing.Extensions.CodeCoverage](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-extensions-code-coverage) – Code coverage collection

## Workflows & Actions

- [GitHub Actions](https://github.com/features/actions) – CI, lint, release and blog automation (`.github/workflows`)

## Development Tools

- [Visual Studio 2022](https://visualstudio.microsoft.com/) – Primary IDE
- [JetBrains Rider](https://www.jetbrains.com/rider/) – Alternative IDE
- [Visual Studio Code](https://code.visualstudio.com/) – Lightweight editor, cross-platform

## Architecture & Patterns

- [Dependency Injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection) – Built-in ASP.NET Core DI container
- [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management) – One pinned version per package (`Directory.Packages.props`)
