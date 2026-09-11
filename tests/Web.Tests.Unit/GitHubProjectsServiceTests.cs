using System.Net;
using Microsoft.Extensions.Options;
using Web.Services;

namespace Web.Tests.Unit;

public class GitHubProjectsServiceTests
{
	[Fact]
	public async Task GetProjectsAsync_NoOwnerConfigured_ReturnsFriendlyError()
	{
		// Arrange
		var service = CreateService(new GitHubProjectsOptions { Owner = "" }, new StubHttpMessageHandler((_, _) =>
			throw new InvalidOperationException("HTTP should not be called when no owner is configured.")));

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(result.Repositories);
		Assert.False(result.IsAuthenticated);
		Assert.Contains("GitHub:Owner", result.ErrorMessage);
	}

	[Fact]
	public async Task GetProjectsAsync_TokenConfigured_ReportsAuthenticatedAndSendsAuthorizationHeader()
	{
		// Arrange
		var sawAuthorizationHeader = false;
		var handler = new StubHttpMessageHandler((request, _) =>
		{
			if (request.Headers.Authorization is { Scheme: "Bearer", Parameter: "test-token" })
			{
				sawAuthorizationHeader = true;
			}

			if (request.RequestUri!.AbsolutePath.EndsWith("/repos", StringComparison.Ordinal))
			{
				return JsonResponse("""[{"name":"repo-a","html_url":"https://github.com/octocat/repo-a"}]""");
			}

			return JsonResponse("[]");
		});

		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat", Token = "test-token" }, handler);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result.IsAuthenticated);
		Assert.True(sawAuthorizationHeader);
		Assert.Single(result.Repositories);
	}

	[Fact]
	public async Task GetProjectsAsync_NoToken_ReportsUnauthenticated()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((request, _) =>
			request.RequestUri!.AbsolutePath.EndsWith("/repos", StringComparison.Ordinal)
				? JsonResponse("[]")
				: JsonResponse("[]"));

		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat" }, handler);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.False(result.IsAuthenticated);
		Assert.Null(result.ErrorMessage);
	}

	[Fact]
	public async Task GetProjectsAsync_FiltersPullRequestsOutOfIssuesAndIntoPullRequests()
	{
		// Arrange
		const string issuesJson = """
			[
				{"number": 1, "title": "A real issue", "html_url": "https://github.com/octocat/repo-a/issues/1"},
				{"number": 2, "title": "A pull request", "html_url": "https://github.com/octocat/repo-a/pull/2", "pull_request": {"url": "https://api.github.com/repos/octocat/repo-a/pulls/2"}}
			]
			""";

		var handler = new StubHttpMessageHandler((request, _) =>
			request.RequestUri!.AbsolutePath.EndsWith("/repos", StringComparison.Ordinal)
				? JsonResponse("""[{"name":"repo-a","html_url":"https://github.com/octocat/repo-a"}]""")
				: JsonResponse(issuesJson));

		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat" }, handler);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		var repository = Assert.Single(result.Repositories);
		var issue = Assert.Single(repository.Issues);
		Assert.Equal("A real issue", issue.Title);
		var pullRequest = Assert.Single(repository.PullRequests);
		Assert.Equal("A pull request", pullRequest.Title);
	}

	[Fact]
	public async Task GetProjectsAsync_OwnerNotFound_ReturnsFriendlyError()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) =>
			new HttpResponseMessage(HttpStatusCode.NotFound));

		var service = CreateService(new GitHubProjectsOptions { Owner = "does-not-exist" }, handler);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(result.Repositories);
		Assert.Contains("does-not-exist", result.ErrorMessage);
	}

	[Fact]
	public async Task GetProjectsAsync_RateLimited_ReturnsFriendlyErrorInsteadOfThrowing()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) =>
			new HttpResponseMessage((HttpStatusCode)403));

		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat" }, handler);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Empty(result.Repositories);
	}

	[Fact]
	public async Task GetProjectsAsync_TransportThrows_ReturnsGracefullyWithoutThrowing()
	{
		// Arrange
		var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("boom"));
		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat" }, handler);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Empty(result.Repositories);
	}

	private static GitHubProjectsService CreateService(GitHubProjectsOptions options, HttpMessageHandler handler)
	{
		var httpClient = new HttpClient(handler);
		var httpClientFactory = new SingleClientHttpClientFactory(httpClient);
		return new GitHubProjectsService(httpClientFactory, Options.Create(options));
	}

	private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
	{
		Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
	};

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
}
