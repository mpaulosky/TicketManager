---
post_title: "Scoping the HTTP Launch-Profile Fix to AppHost.Tests Only"
author1: "mpaulosky"
post_slug: "scoping-the-http-launch-profile-fix"
microsoft_alias: "mpaulosky"
featured_image: ""
categories: ["DevOps", "CI/CD", "Testing"]
tags: ["aspire", "dotnet", "github-actions", "tls"]
ai_note: "This post was drafted with AI assistance (GitHub Copilot / Squad agent) based on the actual repository history."
summary: "The dev-cert fix that saved AppHost.Tests in CI turned out to be too broad, breaking HTTPS for real local dev sessions. Here's how an environment-variable toggle scoped the fix to test runs only."
post_date: "2026-09-09"
---

## Recap: how we got here

Two posts back, `AppHost.cs` was changed to pin the `web` resource to the
`http` launch profile, fixing a TLS `UntrustedRoot` failure in
`AppHost.Tests` on GitHub Actions:

```csharp
builder.AddProject<Projects.Web>("web", launchProfileName: "http");
```

That change (PR #13) merged via auto-merge before a planned follow-up
correction could land. The problem: this line doesn't just affect
`AppHost.Tests` - it affects *every* consumer of `AppHost.cs`, including a
real developer running `dotnet run --project src/AppHost` locally. Pinning
the profile unconditionally to `http` silently broke HTTPS for normal local
dev sessions, not just the CI test run it was meant to fix.

## Why this wasn't just "revert and redo"

The obvious instinct might be to pass the launch profile in conditionally,
something like `launchProfileName: someEnvVar` where `someEnvVar` is `null`
outside of tests. But `AddProject<TProject>(name, launchProfileName)`
doesn't behave as a safe no-op when `launchProfileName` is `null`. Passing
`null` explicitly sets `ExcludeLaunchProfile = true` - excluding launch
profiles entirely - rather than falling back to Aspire's normal default
profile selection. That's a different (and worse) behavior than either the
original default or the `http` pin: it doesn't just skip picking a profile,
it disables using one altogether.

This was verified with a throwaway test confirming the difference in
practice: the parameterless overload (`AddProject<Projects.Web>("web")`)
resolves `web` to both HTTPS and HTTP endpoints (Aspire's original default
behavior), while the `launchProfileName` overload with `"http"` resolves to
HTTP only. Any fix needed to choose between these two overloads explicitly,
rather than trying to thread a possibly-null value through the
`launchProfileName` parameter.

## The actual fix

Commit `4d088ae` (PR #14) branches on an environment variable instead:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Real local/dev runs use Web's default (https) launch profile. AppHost.Tests
// overrides this to "http" via the WEB_LAUNCH_PROFILE env var before it spins
// up the app, since the local ASP.NET Core dev cert isn't trusted on CI runners.
// Passing launchProfileName: null (rather than omitting the argument) sets
// ExcludeLaunchProfile = true, which is not the same as the default -- so the
// override only applies when the env var is actually set.
var webLaunchProfile = Environment.GetEnvironmentVariable("WEB_LAUNCH_PROFILE");
if (string.IsNullOrEmpty(webLaunchProfile))
{
	builder.AddProject<Projects.Web>("web");
}
else
{
	builder.AddProject<Projects.Web>("web", launchProfileName: webLaunchProfile);
}

builder.Build().Run();
```

When `WEB_LAUNCH_PROFILE` is unset - the case for every normal local/dev run
- the code calls the parameterless overload, preserving Aspire's original
default behavior (both HTTPS and HTTP endpoints available, dev cert used as
before). Only when the env var is explicitly set does it call the
`launchProfileName` overload.

On the test side, `AppHost.Tests` now sets that variable on its own process
before spinning up the app host:

```csharp
// Force Web's "http" launch profile for this test process only (not for
// real local/dev `dotnet run`) -- the ASP.NET Core dev cert used by the
// default "https" profile isn't trusted on CI runners.
Environment.SetEnvironmentVariable("WEB_LAUNCH_PROFILE", "http");

var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(cancellationToken);
```

Because `Environment.SetEnvironmentVariable` only affects the current
process, this scopes the override to the test process itself - it can't leak
into a developer's shell or into a real `dotnet run` invocation. CI gets its
trusted, cert-free `http` endpoint; local `dotnet run` gets its original
HTTPS-by-default behavior back.

## The lesson

This is a good example of two correct-looking, narrowly-scoped PRs still
producing a bad outcome when merged in the wrong order: PR #13's fix solved
the CI failure but was broader than it needed to be, and it merged via
auto-merge before the scoped-down version could land first. The
follow-up in PR #14 didn't just "narrow the blast radius" cosmetically - it
required understanding a subtle, undocumented behavior difference between
Aspire's `AddProject` overloads (`null` isn't a no-op) before a genuinely
safe conditional fix was possible. Verifying that behavior with a throwaway
test, rather than assuming it, is what caught the difference before it
shipped as a second bug.
