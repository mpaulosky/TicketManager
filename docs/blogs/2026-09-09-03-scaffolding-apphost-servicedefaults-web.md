---
post_title: "Scaffolding the Aspire Shape: AppHost, ServiceDefaults, and Web"
author1: "mpaulosky"
post_slug: "scaffolding-apphost-servicedefaults-web"
microsoft_alias: "mpaulosky"
featured_image: ""
categories: ["Engineering", "Aspire", ".NET"]
tags: ["aspire", "blazor", "opentelemetry", "scaffolding"]
ai_note: "This post was drafted with AI assistance (GitHub Copilot / Squad agent) based on the actual repository history."
summary: "A walkthrough of PR #8 (commit 641d3b1), which scaffolds the AppHost, ServiceDefaults, and Web projects and wires them into TicketManager.slnx, establishing the standard .NET Aspire application shape for TicketManager."
post_date: "2026-09-09"
---

## What landed in this commit

Commit [641d3b1](https://github.com/mpaulosky/TicketManager/commit/641d3b1f82928d645bad07d088fdbc266dfa6113)
("Scaffold src/ projects: AppHost, ServiceDefaults, Web", PR #8) adds the first three
runnable projects to TicketManager: `src/AppHost`, `src/ServiceDefaults`, and `src/Web`.
Twenty-nine files and 1,032 lines were added, and no existing files were modified other
than `TicketManager.slnx`. This closes ticket #3 ("Scaffold src/ projects (AppHost,
ServiceDefaults, Web) and wire into slnx"), part of the broader src/tests skeleton effort
tracked in ticket #2.

Nothing here is invented business logic yet — this is pure scaffolding. The value of the
commit is establishing the standard .NET Aspire application shape that every future
TicketManager service will plug into.

## What .NET Aspire scaffolding actually is

.NET Aspire is an opinionated, cloud-ready stack for building observable, production-ready
distributed applications. Rather than hand-wiring service discovery, health checks, and
telemetry into every project, Aspire's project templates generate three cooperating pieces:

- an **AppHost** project that acts as the orchestrator, declaring which services exist and
  how they relate to each other,
- a **ServiceDefaults** shared class library that every service references to pick up the
  same cross-cutting behavior, and
- one or more **application** projects that contain the actual product code.

This commit generates exactly that shape for TicketManager, with `Web` as the (currently
only) application project.

## Wiring into TicketManager.slnx

The solution file change is small but important — it is what makes the three new projects
buildable and visible as a unit:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/AppHost/AppHost.csproj" />
    <Project Path="src/ServiceDefaults/ServiceDefaults.csproj" />
    <Project Path="src/Web/Web.csproj" />
  </Folder>
</Solution>
```

Grouping the three projects under a `/src/` solution folder keeps the solution explorer
organized as more projects (and later, `/tests/` folders) are added.

## AppHost: orchestrating the Web resource

`src/AppHost/AppHost.csproj` uses the `Aspire.AppHost.Sdk` (version `13.4.6`) and produces
an executable (`OutputType=Exe`) targeting `net10.0`. Its only project reference is `Web`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Web\Web.csproj" />
</ItemGroup>
```

The orchestration logic itself is tiny — `src/AppHost/AppHost.cs` is five lines:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Web>("web");

builder.Build().Run();
```

`DistributedApplication.CreateBuilder` starts the Aspire app model, `AddProject<Projects.Web>("web")`
registers the Web project as a named resource ("web") that Aspire will launch and monitor,
and `Build().Run()` starts the whole distributed application. When you run AppHost, it
launches the Aspire dashboard alongside the Web project, wiring up service discovery and
telemetry endpoints automatically. The `Properties/launchSettings.json` file backs this up
with `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` and `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL`
environment variables for both `https` and `http` profiles, and `aspire.config.json` simply
records where the AppHost project file lives (`AppHost.csproj`) for Aspire tooling.

As TicketManager grows to include more services (APIs, background workers, databases), they
will each get their own `builder.AddProject<...>(...)` or `builder.AddX(...)` call here —
AppHost is the single place that describes the whole running topology.

## ServiceDefaults: shared cross-cutting configuration

`src/ServiceDefaults/ServiceDefaults.csproj` is a plain class library (`Microsoft.NET.Sdk`)
with `IsAspireSharedProject=true` and a `FrameworkReference` to `Microsoft.AspNetCore.App`,
plus package references for resilience, service discovery, and OpenTelemetry:

```xml
<PackageReference Include="Microsoft.Extensions.Http.Resilience" />
<PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
```

Notice these `PackageReference` entries have no `Version` attributes. That's deliberate: the
commit message explains that "ServiceDefaults' generated PackageReference versions were
stripped to comply with this repo's Central Package Management (Directory.Packages.props
already pins them)". Rather than duplicating version numbers per-project, TicketManager
centralizes them, so the generated Aspire template output had to be adjusted to match that
convention.

The real payload is `src/ServiceDefaults/Extensions.cs` (127 lines), which exposes four
extension methods on `IHostApplicationBuilder`/`WebApplication`:

- **`AddServiceDefaults()`** — the one method every service project calls in `Program.cs`.
  It wires up OpenTelemetry, default health checks, service discovery
  (`AddServiceDiscovery()`), and configures `HttpClient` defaults to use a standard
  resilience handler and service discovery by default.
- **`ConfigureOpenTelemetry()`** — configures logging (`IncludeFormattedMessage`,
  `IncludeScopes`), metrics (ASP.NET Core, HttpClient, and runtime instrumentation), and
  tracing (ASP.NET Core instrumentation, with health-check paths like `/health` and
  `/alive` filtered out of traces so they don't clutter observability data).
- **`AddOpenTelemetryExporters()`** (private) — conditionally enables the OTLP exporter only
  if an `OTEL_EXPORTER_OTLP_ENDPOINT` configuration value is present, so telemetry export is
  opt-in based on environment configuration rather than hardcoded.
- **`AddDefaultHealthChecks()`** — registers a simple "self" liveness check tagged `"live"`.
- **`MapDefaultEndpoints()`** — maps `/health` (all checks must pass) and `/alive` (only
  `"live"`-tagged checks) endpoints, but only when `app.Environment.IsDevelopment()` is
  true, because the code comments call out that exposing health endpoints in non-development
  environments "has security implications".

Every future TicketManager service is expected to reference `ServiceDefaults` and call
`builder.AddServiceDefaults()` plus `app.MapDefaultEndpoints()`, exactly as `Web` does today,
so telemetry, health checks, and resilience behavior stay consistent across the whole
solution without being reimplemented per project.

## Web: the actual application

`src/Web/Web.csproj` uses `Microsoft.NET.Sdk.Web`, targets `net10.0`, and references
`ServiceDefaults`:

```xml
<ItemGroup>
  <ProjectReference Include="..\ServiceDefaults\ServiceDefaults.csproj" />
</ItemGroup>
```

`src/Web/Program.cs` shows the expected consumption pattern end-to-end:

```csharp
using Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
```

This is a standard Blazor Web App using Interactive Server render mode
(`AddInteractiveServerComponents()` / `AddInteractiveServerRenderMode()`), scaffolded with
the usual supporting pieces: `Components/App.razor`, `Components/Routes.razor`,
`Components/_Imports.razor`, a `Layout` folder with `MainLayout`, `NavMenu`, and
`ReconnectModal` (each with matching `.razor.css` scoped styles, and `ReconnectModal.razor.js`
for reconnect-UI scripting), and starter `Pages` (`Home`, `Counter`, `Weather`, `Error`,
`NotFound`). None of this is TicketManager-specific business logic yet — it's the default
Blazor template content that a future PR will replace with real ticket-management screens.

The two calls that matter for this post are `builder.AddServiceDefaults()` at startup and
`app.MapDefaultEndpoints()` in the pipeline — these are exactly the ServiceDefaults hooks
described above, applied to the Web project.

## Why this is the standard Aspire app shape

This three-project split (AppHost / ServiceDefaults / \<app\>) is the shape the Aspire
project templates generate by default, and it exists for a specific reason: it separates
**what runs and how it's wired together** (AppHost) from **what every service does the same
way** (ServiceDefaults) from **what a given service actually does** (Web, and later other
application projects). AppHost's `AddProject<Projects.Web>("web")` call is how compile-time
project references become a runtime-orchestrated resource graph, complete with the Aspire
dashboard, service discovery, and telemetry endpoints — all without Web itself knowing
anything about orchestration.

As TicketManager adds more services — APIs, workers, data stores — they will follow the same
pattern: a `ProjectReference` to `ServiceDefaults`, a call to `AddServiceDefaults()` /
`MapDefaultEndpoints()`, and a corresponding `builder.AddProject<...>(...)` line in
`AppHost.cs`. This commit establishes that pattern once so every subsequent service project
can just follow it.
