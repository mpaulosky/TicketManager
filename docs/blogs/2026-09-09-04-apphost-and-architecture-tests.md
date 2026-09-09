---
post_title: "Testing the Whole Distributed App: AppHost.Tests and Architecture.Tests"
author1: "mpaulosky"
post_slug: "apphost-and-architecture-tests"
microsoft_alias: "mpaulosky"
featured_image: ""
categories: ["Testing", "Engineering", "Architecture"]
tags: ["aspire", "netarchtest", "xunit", "dotnet"]
ai_note: "This post was drafted with AI assistance (GitHub Copilot / Squad agent) based on the actual repository history."
summary: "PR #9 scaffolds two new test projects for TicketManager: AppHost.Tests, which boots the real distributed application with Aspire.Hosting.Testing, and Architecture.Tests, which uses NetArchTest.Rules to enforce a dependency constraint on the Web project."
post_date: "2026-09-09"
---

## Why a distributed-app template needs more than unit tests

[TicketManager](/home/teqs/github/TicketManager) is built on .NET Aspire, which means the "application"
isn't one process — it's an orchestrated set of resources (the `AppHost`, the `Web` front end, and whatever
services get added later) wired together by service discovery, health checks, and configuration. Unit tests
alone can't catch a mistake in how those pieces are wired: a missing resource reference, a broken health
check, or a service that never becomes ready. [PR #9](https://github.com/mpaulosky/TicketManager/pull/9),
commit `0cfe2d7`, addresses that gap by scaffolding two new test projects: `AppHost.Tests` and
`Architecture.Tests`.

## AppHost.Tests: booting the real distributed app

[tests/AppHost.Tests/AppHostTests.cs](/home/teqs/github/TicketManager/tests/AppHost.Tests/AppHostTests.cs) uses
`Aspire.Hosting.Testing`'s `DistributedApplicationTestingBuilder` to spin up the actual `AppHost` project
in-process, the same way it would run in production, and then exercise it over HTTP:

```csharp
[Fact]
public async Task GetWebResourceRootReturnsOkStatusCode()
{
    // Arrange
    var cancellationToken = TestContext.Current.CancellationToken;
    var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(cancellationToken);
    appHost.Services.AddLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Debug);
        logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
        logging.AddFilter("Aspire.", LogLevel.Debug);
    });
    appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
    {
        clientBuilder.AddStandardResilienceHandler();
    });

    await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
    await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

    // Act
    using var httpClient = app.CreateHttpClient("web");
    await app.ResourceNotifications.WaitForResourceHealthyAsync("web", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
    using var response = await httpClient.GetAsync("/", cancellationToken);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

A few details are worth calling out:

- `DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>` builds the same Aspire application
  model that runs in production, referenced directly via a `ProjectReference` to
  `src/AppHost/AppHost.csproj` — there's no separate "test topology" to keep in sync.
- `app.ResourceNotifications.WaitForResourceHealthyAsync("web", ...)` waits for Aspire's own health-check
  machinery to report the `web` resource as healthy before the test makes an HTTP call. This is the key
  difference from a plain integration test: it validates the orchestration layer (resource startup order,
  health probes) in addition to the HTTP response.
- Every asynchronous step is wrapped in `.WaitAsync(DefaultTimeout, cancellationToken)` with a 60-second
  timeout, so a hung resource fails the test instead of hanging CI indefinitely.
- The client is created with `app.CreateHttpClient("web")`, which resolves the resource's endpoint through
  Aspire's service discovery rather than a hardcoded URL — the same mechanism the real `AppHost` uses to
  connect resources to each other.

## Architecture.Tests: enforcing a dependency rule, not just behavior

[tests/Architecture.Tests/WebArchitectureTests.cs](/home/teqs/github/TicketManager/tests/Architecture.Tests/WebArchitectureTests.cs)
takes a completely different angle. Instead of asserting what the app *does*, it asserts what the code is
*allowed to depend on*, using `NetArchTest.Rules`:

```csharp
[Fact]
public void Web_DoesNotReferenceSqlClientAssemblies()
{
    // Arrange
    var webAssembly = typeof(Web.Components.App).Assembly;

    // Act
    var result = Types.InAssembly(webAssembly)
        .Should()
        .NotHaveDependencyOnAny("System.Data.SqlClient", "Microsoft.Data.SqlClient")
        .GetResult();

    // Assert
    Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
}
```

This test loads the `Web` assembly by reflection (`typeof(Web.Components.App).Assembly`) and checks that no
type in it has a dependency on `System.Data.SqlClient` or `Microsoft.Data.SqlClient`. The rule is a direct,
executable encoding of the "no local database" constraint documented in `docs/ARCHITECTURE.md` — instead of
relying on a reviewer to notice a stray `using Microsoft.Data.SqlClient;` in a PR, the build now fails
automatically if that dependency creeps in. The assertion failure message
(`string.Join(", ", result.FailingTypeNames ?? [])`) also names the offending types, so a future violation
is easy to diagnose without re-running the rule manually.

## Why both test types matter together

These two projects check different failure modes, and neither substitutes for the other:

- **AppHost.Tests** answers "does the system actually start up and serve traffic the way it's wired
  together?" — it's an end-to-end smoke test for the orchestration layer itself.
- **Architecture.Tests** answers "does the code still respect the structural constraints we decided on?" —
  it's a static, structural check that runs in milliseconds and catches drift long before it becomes a
  runtime problem.

For a distributed-app template project like TicketManager, where the whole point is that new
services and resources will be added over time, having both a runtime smoke test and an automated
architecture rule gives early warning on the two most common ways a template like this rots: broken wiring
and drifted boundaries.

## Shared testing conventions

Both projects follow the repository's established testing convention: `xunit.v3.mtp-v2` for the test
framework plus `Microsoft.Testing.Extensions.CodeCoverage` for coverage collection, with package versions
centrally pinned in `Directory.Packages.props`. This keeps every test project in the solution — regardless
of what it's testing — consistent in how it's built, run, and measured.
