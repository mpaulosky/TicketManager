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
