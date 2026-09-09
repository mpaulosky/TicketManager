---
post_title: "Fixing an Untrusted HTTPS Dev Cert Failure in AppHost.Tests"
author1: "mpaulosky"
post_slug: "fixing-untrusted-https-dev-cert-in-ci"
microsoft_alias: "mpaulosky"
featured_image: ""
categories: ["DevOps", "CI/CD", "Testing"]
tags: ["aspire", "dotnet", "github-actions", "tls"]
ai_note: "This post was drafted with AI assistance (GitHub Copilot / Squad agent) based on the actual repository history."
summary: "AppHost.Tests started failing in GitHub Actions with an UntrustedRoot TLS error. Here's why Aspire's default launch profile resolution triggered it, and the one-line fix that pinned the Web resource to HTTP."
post_date: "2026-09-09"
---

## The symptom

`AppHost.Tests` in TicketManager passed locally but failed reliably on
GitHub-hosted runners with a TLS handshake error: `UntrustedRoot`. Nothing in
the test itself had changed - it was simply spinning up the Aspire
`AppHost` and asking the `web` resource to respond over HTTP.

## Root cause

The `AppHost.cs` entry point registered the `Web` project like this:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Web>("web");

builder.Build().Run();
```

`AddProject<Projects.Web>("web")` has no explicit launch profile argument,
so .NET Aspire falls back to resolving the resource against `Web`'s own
`launchSettings.json` - and that project's default profile is `https`.
Locally, that's invisible: the ASP.NET Core developer certificate
(`dotnet dev-certs https --trust`) is already trusted on a dev machine, so the
TLS handshake succeeds silently.

On a GitHub-hosted runner, though, there is no trusted dev cert. The
`AppHost.Tests` project talks to the resolved `web` endpoint through a
resilience-wrapped `HttpClient`, and that client's TLS validation rejects the
self-signed ASP.NET Core dev certificate as an `UntrustedRoot`. The test
failure had nothing to do with test logic - it was purely an artifact of which
launch profile Aspire picked for the test run.

## The fix

The fix committed in `b616885` (PR #13) pinned the resource to the `http`
launch profile explicitly:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Web>("web", launchProfileName: "http");

builder.Build().Run();
```

The reasoning behind choosing this fix over alternatives (like adding a
CI-specific dev-cert-trust step) was pragmatic: TicketManager doesn't have a
real HTTPS requirement yet - Tailwind and auth were both already deferred
per the project's own roadmap notes - so there was no reason to solve for TLS
trust in CI at all. Pinning the profile to `http` is also **portable**: it
produces identical behavior locally and in CI, with no extra CI-only step to
maintain or accidentally skip.

## Why this mattered beyond the immediate fix

This one-line change is deceptively simple, but it's worth internalizing the
underlying behavior: Aspire's `AddProject<TProject>(name)` overload doesn't
default to "no profile" - it defaults to *whatever profile the referenced
project's `launchSettings.json` lists*, which for a typical ASP.NET Core web
project is `https`. Any Aspire-orchestrated test that doesn't need HTTPS
should either explicitly select `http`, or run somewhere the ambient dev cert
is already trusted.

As we'll see in the next two posts, this same one-line pin turned out to
have a broader blast radius than intended, when it collided with another PR
in flight and needed a follow-up correction to properly scope it to test runs
only.
