---
post_title: "Three Kinds of Web Tests: Unit, bUnit, and Integration Baselines"
author1: "mpaulosky"
post_slug: "web-unit-bunit-integration-tests"
microsoft_alias: "mpaulosky"
featured_image: ""
categories: ["Testing", "Engineering", "Quality"]
tags: ["xunit", "bunit", "aspnetcore", "dotnet"]
ai_note: "This post was drafted with AI assistance (GitHub Copilot / Squad agent) based on the actual repository history."
summary: "PR #10 rounds out TicketManager's Web test coverage with three focused projects — Web.Tests.Unit, Web.Tests.Bunit, and Web.Tests.Integration — each carrying one real smoke test against the default template output."
post_date: "2026-09-09"
---

## Filling out the Web test pyramid

[PR #10](https://github.com/mpaulosky/TicketManager/pull/10), commit `bd0bd92`, adds the three remaining
test projects for the `Web` project in [TicketManager](/home/teqs/github/TicketManager):
`Web.Tests.Unit`, `Web.Tests.Bunit`, and `Web.Tests.Integration`. Together with `AppHost.Tests` and
`Architecture.Tests` from the previous PR, this completes the test-project skeleton called for by the
"Scaffold TicketManager src/ and tests/ project skeleton" Wayfinder map. Each new project targets a
distinct layer of the application and ships with exactly one smoke test — enough to prove the harness
works end to end without overcommitting to test cases the template doesn't need yet.

## Web.Tests.Unit: configuration in isolation

[tests/Web.Tests.Unit/AppSettingsTests.cs](/home/teqs/github/TicketManager/tests/Web.Tests.Unit/AppSettingsTests.cs)
is a plain unit test with no ASP.NET Core host involved at all:

```csharp
[Fact]
public void AppSettings_AllowedHosts_IsWildcard()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json")
        .Build();

    // Act
    var allowedHosts = configuration["AllowedHosts"];

    // Assert
    Assert.Equal("*", allowedHosts);
}
```

It loads `appsettings.json` directly through `Microsoft.Extensions.Configuration.Json` and asserts a single
configuration value. There's no server, no HTTP pipeline, no rendering — just a fast, deterministic check
that the shipped configuration file has the expected `AllowedHosts` value. This is the cheapest and fastest
tier of the pyramid: it runs in milliseconds and fails only when the configuration itself changes.

## Web.Tests.Bunit: rendering a component without a browser

[tests/Web.Tests.Bunit/HomeTests.cs](/home/teqs/github/TicketManager/tests/Web.Tests.Bunit/HomeTests.cs)
uses the `bunit` package to render a real Blazor component in memory and inspect its output:

```csharp
public class HomeTests : BunitContext
{
    [Fact]
    public void Home_Rendered_ShowsDefaultTemplateMarkup()
    {
        // Arrange & Act
        var cut = Render<Home>();

        // Assert
        Assert.Equal("Hello, world!", cut.Find("h1").TextContent);
        Assert.Contains("Welcome to your new app.", cut.Markup);
    }
}
```

By inheriting `BunitContext`, the test gets a component renderer that can mount `Web.Components.Pages.Home`
directly, find elements by CSS-style selectors (`cut.Find("h1")`), and inspect the resulting markup
(`cut.Markup`) — all without spinning up Kestrel or a browser. This is the layer that unit tests and
integration tests can't cover on their own: it verifies that a specific Razor component renders the
expected DOM structure and content, which matters for anything with conditional markup, event handlers, or
component parameters as the UI grows.

## Web.Tests.Integration: the app through its real HTTP pipeline

[tests/Web.Tests.Integration/WebApplicationTests.cs](/home/teqs/github/TicketManager/tests/Web.Tests.Integration/WebApplicationTests.cs)
uses `Microsoft.AspNetCore.Mvc.Testing`'s `WebApplicationFactory<Program>` to host the actual `Web`
application in-process and issue a real HTTP request against it:

```csharp
public class WebApplicationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebApplicationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Root_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

This exercises the full ASP.NET Core middleware pipeline — routing, Razor component rendering, response
generation — for a `GET /` request and asserts a `200 OK`. Unlike `AppHost.Tests`, which boots the entire
Aspire distributed application (AppHost plus every resource it orchestrates), this test hosts only the
`Web` project itself via `WebApplicationFactory`, making it faster and more focused on the web tier in
isolation. Making that possible required one small production change: [src/Web/Program.cs](/home/teqs/github/TicketManager/src/Web/Program.cs)
gained a `public partial class Program;` marker at the bottom of the file, since `WebApplicationFactory<T>`
needs a public type in the entry-point assembly to bind to — top-level statement programs don't expose one
by default.

## Why smoke tests are the right starting point

Each of these three projects ships with a single, narrowly-scoped test rather than broad coverage of
every page or component. That's deliberate: at this stage the `Web` project is still the default Aspire
template output, so there isn't yet meaningful application-specific behavior to test. What the smoke tests
prove is that the *harness* for each testing style is correctly wired — project references, package
references, and test-runner configuration all work — so that when real features land, the first test for
each of them can be added directly without first debugging the test infrastructure itself. Combined with
the unit/bUnit/integration split, the template now has one working example test per layer: fast and
isolated (`Web.Tests.Unit`), component-focused without a browser (`Web.Tests.Bunit`), and full-pipeline
(`Web.Tests.Integration`) — a concrete pattern for every future test to follow.

## Consistent conventions across all three

Like `AppHost.Tests` and `Architecture.Tests`, all three new projects use the same
`xunit.v3.mtp-v2` and `Microsoft.Testing.Extensions.CodeCoverage` combination, with package versions
resolved from `Directory.Packages.props` rather than pinned per project. Every project is also registered
in `TicketManager.slnx` under the `/tests/` folder, so `dotnet test TicketManager.slnx` picks up all five
test projects — `AppHost.Tests`, `Architecture.Tests`, `Web.Tests.Unit`, `Web.Tests.Bunit`, and
`Web.Tests.Integration` — in a single run.
