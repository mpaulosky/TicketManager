using Microsoft.Extensions.Options;
using Web.Services;

namespace Web.Tests.Unit;

public class GitHubProjectsServiceTests
{
	[Fact]
	public async Task GetProjectsAsync_NoOwnerConfigured_ReturnsFriendlyError()
	{
		// Arrange
		var service = CreateService(new GitHubProjectsOptions { Owner = "" },
			new FakeGitHubRestClient((_, _) => throw new InvalidOperationException(
				"GitHub should not be called when no owner is configured.")));

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(result.Repositories);
		Assert.False(result.IsAuthenticated);
		Assert.Contains("GitHub:Owner", result.ErrorMessage);
	}

	[Fact]
	public async Task GetProjectsAsync_TokenConfigured_ReportsAuthenticatedAndPassesTokenThrough()
	{
		// Arrange
		string? capturedToken = null;
		var restClient = new FakeGitHubRestClient((path, token) =>
		{
			if (path.EndsWith("/repos?per_page=100&sort=updated", StringComparison.Ordinal))
			{
				capturedToken = token;
				return new List<GitHubRepositoryDto>
				{
					new() { Name = "repo-a", HtmlUrl = "https://github.com/octocat/repo-a" },
				};
			}

			return new List<GitHubIssueDto>();
		});

		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat", Token = "test-token" }, restClient);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result.IsAuthenticated);
		Assert.Equal("test-token", capturedToken);
		Assert.Single(result.Repositories);
	}

	[Fact]
	public async Task GetProjectsAsync_NoToken_ReportsUnauthenticated()
	{
		// Arrange
		var restClient = new FakeGitHubRestClient((path, _) =>
			path.EndsWith("/repos?per_page=100&sort=updated", StringComparison.Ordinal)
				? new List<GitHubRepositoryDto>()
				: new List<GitHubIssueDto>());

		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat" }, restClient);

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
		object RepositoryList() =>
			new List<GitHubRepositoryDto>
			{
				new() { Name = "repo-a", HtmlUrl = "https://github.com/octocat/repo-a" },
			};

		object IssuesList() =>
			new List<GitHubIssueDto>
			{
				new()
				{
					Number = 1, Title = "A real issue", HtmlUrl = "https://github.com/octocat/repo-a/issues/1",
				},
				new()
				{
					Number = 2, Title = "A pull request", HtmlUrl = "https://github.com/octocat/repo-a/pull/2",
					PullRequest = new { url = "https://api.github.com/repos/octocat/repo-a/pulls/2" },
				},
			};

		var restClient = new FakeGitHubRestClient((path, _) =>
			path.EndsWith("/repos?per_page=100&sort=updated", StringComparison.Ordinal)
				? RepositoryList()
				: IssuesList());

		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat" }, restClient);

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
	public async Task GetProjectsAsync_OwnerNotFoundOnEitherSegment_ReturnsFriendlyError()
	{
		// Arrange
		var restClient = new FakeGitHubRestClient((_, _) => null);
		var service = CreateService(new GitHubProjectsOptions { Owner = "does-not-exist" }, restClient);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(result.Repositories);
		Assert.Contains("does-not-exist", result.ErrorMessage);
	}

	[Fact]
	public async Task GetProjectsAsync_PreferredSegmentFails_FallsBackToTheOtherSegment()
	{
		// Arrange
		var restClient = new FakeGitHubRestClient((path, _) =>
		{
			if (path.StartsWith("users/", StringComparison.Ordinal))
			{
				return null;
			}

			if (path.StartsWith("orgs/", StringComparison.Ordinal))
			{
				return new List<GitHubRepositoryDto>
				{
					new() { Name = "repo-a", HtmlUrl = "https://github.com/octocat/repo-a" },
				};
			}

			return new List<GitHubIssueDto>();
		});

		var service = CreateService(new GitHubProjectsOptions { Owner = "octocat", OwnerType = "User" }, restClient);

		// Act
		var result = await service.GetProjectsAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Single(result.Repositories);
		Assert.Null(result.ErrorMessage);
	}

	private static GitHubProjectsService CreateService(GitHubProjectsOptions options, IGitHubRestClient restClient) =>
		new(restClient, Options.Create(options));

	private sealed class FakeGitHubRestClient(Func<string, string?, object?> responder) : IGitHubRestClient
	{
		public Task<T?> TryGetAsync<T>(string path, string? token = null,
			CancellationToken cancellationToken = default) =>
			Task.FromResult((T?)responder(path, token));
	}
}
