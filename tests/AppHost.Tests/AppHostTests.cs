using System.Net;
using Microsoft.Extensions.Logging;

namespace AppHost.Tests.Tests;

public class AppHostTests
{
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

	[Fact]
	public async Task GetWebResourceRootReturnsOkStatusCode()
	{
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;

		// Force Web's "http" launch profile for this test process only (not for
		// real local/dev `dotnet run`) -- the ASP.NET Core dev cert used by the
		// default "https" profile isn't trusted on CI runners.
		Environment.SetEnvironmentVariable("WEB_LAUNCH_PROFILE", "http");

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
}
