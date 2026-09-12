// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitHubRestClientTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Unit
// =============================================

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
		var result = await client.TryGetAsync<Repo>("repos/octocat/repo-a",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Should().NotBeNull();
		result.Name.Should().Be("repo-a");
		logger.Warnings.Should().BeEmpty();
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
		capturedRequest.Should().NotBeNull();
		capturedRequest.RequestUri!.ToString().Should().Be("https://api.github.com/repos/octocat/repo-a");
		capturedRequest.Headers.Accept.Should().Contain(value => value.MediaType == "application/vnd.github+json");
		capturedRequest.Headers.UserAgent.Should()
			.Contain(value => value.Product != null && value.Product.Name == "TicketManager-Web");
		capturedRequest.Headers.Authorization.Should().BeNull();
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
		capturedRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
		capturedRequest.Headers.Authorization.Parameter.Should().Be("test-token");
	}

	[Fact]
	public async Task TryGetAsync_NonSuccessStatusCode_ReturnsNullAndLogsWarning()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));
		var (client, logger) = CreateClient(handler);

		// Act
		var result = await client.TryGetAsync<Repo>("repos/octocat/missing",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
		logger.Warnings.Should().ContainSingle();
	}

	[Fact]
	public async Task TryGetAsync_TransportThrows_ReturnsNullAndLogsWarning()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("boom"));
		var (client, logger) = CreateClient(handler);

		// Act
		var result = await client.TryGetAsync<Repo>("repos/octocat/repo-a",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
		logger.Warnings.Should().ContainSingle();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task TryGetAsync_NullOrWhitespacePath_ThrowsArgumentException(string? path)
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) =>
			throw new InvalidOperationException("The HTTP client should not be invoked for an invalid path."));
		var (client, _) = CreateClient(handler);

		// Act
		var act = async () => await client.TryGetAsync<Repo>(path!, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		await act.Should().ThrowAsync<ArgumentException>();
	}

	[Fact]
	public async Task TryGetAsync_MalformedJsonBody_ReturnsNullAndLogsWarning()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json"),
		});
		var (client, logger) = CreateClient(handler);

		// Act
		var result = await client.TryGetAsync<Repo>("repos/octocat/repo-a",
			cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
		logger.Warnings.Should().ContainSingle();
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
