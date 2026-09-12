using System.Net;
using Microsoft.Extensions.Logging;
using Web.Services;

namespace Web.Tests.Unit;

public class GitHubRestClientTests
{
	[Fact]
	public async Task TryGetAsync_SuccessfulResponse_ReturnsDeserializedValue()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) => JsonResponse("""{"name":"repo-a"}"""));
		var (client, logger) = CreateClient(handler);

		// Act
		var result = await client.TryGetAsync<Repo>("repos/octocat/repo-a", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("repo-a", result.Name);
		Assert.Empty(logger.Warnings);
	}

	[Fact]
	public async Task TryGetAsync_RequestsExpectedUrlWithAcceptAndUserAgentHeaders()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;
		var handler = new StubHttpMessageHandler((request, _) =>
		{
			capturedRequest = request;
			return JsonResponse("{}");
		});
		var (client, _) = CreateClient(handler);

		// Act
		await client.TryGetAsync<Repo>("repos/octocat/repo-a", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(capturedRequest);
		Assert.Equal("https://api.github.com/repos/octocat/repo-a", capturedRequest.RequestUri!.ToString());
		Assert.Contains(capturedRequest.Headers.Accept, value => value.MediaType == "application/vnd.github+json");
		Assert.Contains(capturedRequest.Headers.UserAgent, value => value.Product?.Name == "TicketManager-Web");
		Assert.Null(capturedRequest.Headers.Authorization);
	}

	[Fact]
	public async Task TryGetAsync_TokenProvided_AttachesBearerAuthorizationHeader()
	{
		// Arrange
		HttpRequestMessage? capturedRequest = null;
		var handler = new StubHttpMessageHandler((request, _) =>
		{
			capturedRequest = request;
			return JsonResponse("{}");
		});
		var (client, _) = CreateClient(handler);

		// Act
		await client.TryGetAsync<Repo>("repos/octocat/repo-a", "test-token", TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal("Bearer", capturedRequest!.Headers.Authorization!.Scheme);
		Assert.Equal("test-token", capturedRequest.Headers.Authorization.Parameter);
	}

	[Fact]
	public async Task TryGetAsync_NonSuccessStatusCode_ReturnsNullAndLogsWarning()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));
		var (client, logger) = CreateClient(handler);

		// Act
		var result = await client.TryGetAsync<Repo>("repos/octocat/missing", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Assert.Null(result);
		Assert.Single(logger.Warnings);
	}

	[Fact]
	public async Task TryGetAsync_TransportThrows_ReturnsNullAndLogsWarning()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("boom"));
		var (client, logger) = CreateClient(handler);

		// Act
		var result = await client.TryGetAsync<Repo>("repos/octocat/repo-a", cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		Assert.Null(result);
		Assert.Single(logger.Warnings);
	}

	private static (GitHubRestClient Client, RecordingLogger<GitHubRestClient> Logger) CreateClient(
		HttpMessageHandler handler)
	{
		var httpClient = new HttpClient(handler);
		var logger = new RecordingLogger<GitHubRestClient>();
		return (new GitHubRestClient(new SingleClientHttpClientFactory(httpClient), logger), logger);
	}

	private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
	{
		Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
	};

	private sealed class Repo
	{
		public string Name { get; set; } = string.Empty;
	}

	private sealed class SingleClientHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => httpClient;
	}

	private sealed class StubHttpMessageHandler(
		Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) => Task.FromResult(responder(request, cancellationToken));
	}

	private sealed class RecordingLogger<T> : ILogger<T>
	{
		public List<string> Warnings { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (logLevel == LogLevel.Warning)
			{
				Warnings.Add(formatter(state, exception));
			}
		}
	}
}
