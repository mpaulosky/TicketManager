---
post_title: "Scaffolding Real E2E Tests and Fixing a Hardcoded CI Artifact Path"
author1: "mpaulosky"
post_slug: "e2e-tests-and-ci-artifact-path-fix"
microsoft_alias: "mpaulosky"
featured_image: ""
categories: ["DevOps", "CI/CD", "Testing"]
tags: ["playwright", "xunit", "github-actions", "dotnet"]
ai_note: "This post was drafted with AI assistance (GitHub Copilot / Squad agent) based on the actual repository history."
summary: "Adding a real Playwright-driven E2E test project to TicketManager surfaced a stale, hardcoded artifact path in ci.yml left over from an earlier project layout - here's what the new smoke test verifies and how the CI path got fixed."
post_date: "2026-09-09"
---

## What this PR added

Commit `ffb5715` (PR #11) scaffolded a new `tests/Web.Tests.E2E` project and
wired it into `TicketManager.slnx`. Unlike the existing unit, bUnit, and
integration test projects, this one drives a real, out-of-process browser
against a real, running instance of `Web`.

## The smoke test itself

The test is intentionally small - a single smoke test confirming the app's
default template renders correctly:

```csharp
namespace Web.Tests.E2E;

public class HomePageTests : PageTest, IClassFixture<WebFactory>
{
	private readonly WebFactory _factory;

	public HomePageTests(WebFactory factory)
	{
		_factory = factory;
		_factory.EnsureStarted();
	}

	public override BrowserNewContextOptions ContextOptions()
	{
		var options = base.ContextOptions();
		options.BaseURL = _factory.ServerAddress;
		return options;
	}

	[Fact]
	public async Task Get_Root_ShowsDefaultTemplateTitleAndContent()
	{
		// Act
		await Page.GotoAsync("/");

		// Assert
		Assert.Equal("Home", await Page.TitleAsync());
		Assert.Contains("Hello, world!", await Page.ContentAsync());
	}
}
```

It navigates to `/` and asserts on the page title and rendered content -
confirming that `Web` boots, serves HTML, and renders its default Blazor
template end-to-end, through a real browser, over a real HTTP connection.
That's a meaningfully different guarantee than a unit or integration test:
it exercises the actual hosting pipeline, static assets, and rendered markup
the way a user's browser would see them.

## Why `WebApplicationFactory` alone wasn't enough

Playwright drives an *external* browser process, which can't reach into an
in-memory `TestServer`. So the PR adds a `WebFactory`:

```csharp
public sealed class WebFactory : WebApplicationFactory<Program>
{
	private bool _started;

	public string ServerAddress { get; private set; } = string.Empty;

	public void EnsureStarted()
	{
		if (_started)
		{
			return;
		}
		// ... starts Web on a real Kestrel TCP port ...
	}
}
```

`WebFactory` hosts `Web` on a real Kestrel TCP port (via
`WebApplicationFactory.UseKestrel`/`StartServer`, available on .NET 10)
instead of the default in-memory `TestServer`, so the out-of-process
Playwright browser has an actual address to navigate to.

## A package collision along the way

The initially pinned `C3D.Extensions.Playwright.AspNetCore.Xunit` package
(0.1.45) transitively pulled in xUnit v2 core through
`C3D.Extensions.Logging.Xunit`, which collided with the repo's
`xunit.v3.mtp-v2` convention - duplicate `IClassFixture<T>`/`FactAttribute`
types across `xunit.core` 2.x and `xunit.v3.core`. The fix was to use
`Microsoft.Playwright.Xunit.v3`'s `PageTest` base class instead, which is
xUnit v3-native and avoids the collision entirely.

That package choice had a second-order effect: `Microsoft.Playwright.Xunit.v3`
1.62.0 requires `Microsoft.Playwright` >= 1.62.0, so the repo's existing pin
of `Microsoft.Playwright` at 1.59.0 in `Directory.Packages.props` caused an
`NU1605` downgrade error. The PR bumped it to 1.62.0 to match.

## The hardcoded CI artifact path

Separately - and this is the part with the more interesting DevOps lesson -
`ci.yml` already had a step for uploading Playwright diagnostics on failure,
left over from an earlier (or planned) project layout:

```yaml
- if: needs.changes.outputs.code == 'true' && matrix.requires_playwright
  with:
    name: playwright-artifacts-${{ matrix.test_id }}
    path: tests/E2E.Tests/bin/Release/**/TestResults/playwright-artifacts/
    if-no-files-found: ignore
```

The path pointed at `tests/E2E.Tests/...`, a project that doesn't exist in
this repo. Since the actual E2E project didn't exist yet either, this
mismatch was silent - `if-no-files-found: ignore` meant the step just quietly
uploaded nothing. Now that `tests/Web.Tests.E2E` is a real project, the path
was corrected to match:

```yaml
path: tests/Web.Tests.E2E/bin/Release/**/TestResults/playwright-artifacts/
```

Without this fix, any future Playwright failure in CI would have produced no
diagnostic artifacts at all - the workflow would report a failing test with
no screenshots or traces to explain why, because the upload step would
always be looking in the wrong place.

## A quieter but real risk

This PR merged shortly after the dev-cert fix from the previous post (PR
#13), while both were in flight around the same time. PR #13's fix -
unconditionally pinning `Web`'s launch profile to `http` in `AppHost.cs` -
merged via auto-merge before a planned follow-up correction could land,
and that correction turned out to be too broad for real local development.
That collision, and the fix for it, is the subject of the next post.
