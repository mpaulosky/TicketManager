// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitHubRepositoriesServiceTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Unit
// =============================================

using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Unit;

public class GitHubRepositoriesServiceTests
{
	[Fact]
	public async Task GetRepositoriesAsync_NoOwnerConfigured_ReturnsFriendlyError()
	{
		// Arrange
		var service = CreateService(new GitHubRepositoriesOptions { Owner = "" },
			new FakeGitHubRestClient((_, _) => throw new InvalidOperationException(
				"GitHub should not be called when no owner is configured.")));

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		result.Repositories.Should().BeEmpty();
		result.IsAuthenticated.Should().BeFalse();
		result.ErrorMessage.Should().Contain("GitHub:Owner");
	}

	[Fact]
	public async Task GetRepositoriesAsync_OwnerNotConfigured_ReturnsFriendlyError()
	{
		// Arrange
		var service = CreateService(new GitHubRepositoriesOptions { Owner = null },
			new FakeGitHubRestClient((_, _) => throw new InvalidOperationException(
				"GitHub should not be called when no owner is configured.")));

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		result.Repositories.Should().BeEmpty();
		result.IsAuthenticated.Should().BeFalse();
		result.ErrorMessage.Should().Contain("GitHub:Owner");
	}

	[Fact]
	public async Task GetRepositoriesAsync_OwnerIsWhitespaceOnly_ReturnsFriendlyError()
	{
		// Arrange
		var service = CreateService(new GitHubRepositoriesOptions { Owner = "   " },
			new FakeGitHubRestClient((_, _) => throw new InvalidOperationException(
				"GitHub should not be called when no owner is configured.")));

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		result.Repositories.Should().BeEmpty();
		result.ErrorMessage.Should().Contain("GitHub:Owner");
	}

	[Fact]
	public async Task GetRepositoriesAsync_ForksAndArchivedRepositories_AreExcluded()
	{
		// Arrange
		var restClient = new FakeGitHubRestClient((path, _) =>
		{
			if (path.EndsWith("/repos?per_page=100&sort=updated", StringComparison.Ordinal))
			{
				return new List<GitHubRepositoryDto>
				{
					new() { Name = "kept", HtmlUrl = "https://github.com/octocat/kept" },
					new() { Name = "a-fork", HtmlUrl = "https://github.com/octocat/a-fork", Fork = true },
					new() { Name = "archived", HtmlUrl = "https://github.com/octocat/archived", Archived = true },
				};
			}

			return new List<GitHubIssueDto>();
		});

		var service = CreateService(new GitHubRepositoriesOptions { Owner = "octocat" }, restClient);

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		result.Repositories.Should().ContainSingle().Which.Name.Should().Be("kept");
	}

	[Fact]
	public async Task GetRepositoriesAsync_TokenConfigured_ReportsAuthenticatedAndPassesTokenThrough()
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

		var service = CreateService(new GitHubRepositoriesOptions { Owner = "octocat", Token = "test-token" }, restClient);

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		result.IsAuthenticated.Should().BeTrue();
		capturedToken.Should().Be("test-token");
		result.Repositories.Should().ContainSingle();
	}

	[Fact]
	public async Task GetRepositoriesAsync_NoToken_ReportsUnauthenticated()
	{
		// Arrange
		var restClient = new FakeGitHubRestClient((path, _) =>
			path.EndsWith("/repos?per_page=100&sort=updated", StringComparison.Ordinal)
				? new List<GitHubRepositoryDto>()
				: new List<GitHubIssueDto>());

		var service = CreateService(new GitHubRepositoriesOptions { Owner = "octocat" }, restClient);

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		result.IsAuthenticated.Should().BeFalse();
		result.ErrorMessage.Should().BeNull();
	}

	[Fact]
	public async Task GetRepositoriesAsync_FiltersPullRequestsOutOfIssuesAndIntoPullRequests()
	{
		// Arrange
		object RepositoryList() =>
			new List<GitHubRepositoryDto> { new() { Name = "repo-a", HtmlUrl = "https://github.com/octocat/repo-a" }, };

		object IssuesList() =>
			new List<GitHubIssueDto>
			{
				new() { Number = 1, Title = "A real issue", HtmlUrl = "https://github.com/octocat/repo-a/issues/1", },
				new()
				{
					Number = 2,
					Title = "A pull request",
					HtmlUrl = "https://github.com/octocat/repo-a/pull/2",
					PullRequest = new { url = "https://api.github.com/repos/octocat/repo-a/pulls/2" },
				},
			};

		var restClient = new FakeGitHubRestClient((path, _) =>
			path.EndsWith("/repos?per_page=100&sort=updated", StringComparison.Ordinal)
				? RepositoryList()
				: IssuesList());

		var service = CreateService(new GitHubRepositoriesOptions { Owner = "octocat" }, restClient);

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		var repository = result.Repositories.Should().ContainSingle().Which;
		var issue = repository.Issues.Should().ContainSingle().Which;
		issue.Title.Should().Be("A real issue");
		var pullRequest = repository.PullRequests.Should().ContainSingle().Which;
		pullRequest.Title.Should().Be("A pull request");
	}

	[Fact]
	public async Task GetRepositoriesAsync_OwnerNotFoundOnEitherSegment_ReturnsFriendlyError()
	{
		// Arrange
		var restClient = new FakeGitHubRestClient((_, _) => null);
		var service = CreateService(new GitHubRepositoriesOptions { Owner = "does-not-exist" }, restClient);

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		result.Repositories.Should().BeEmpty();
		result.ErrorMessage.Should().Contain("does-not-exist");
	}

	[Fact]
	public async Task GetRepositoriesAsync_PreferredSegmentFails_FallsBackToTheOtherSegment()
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

		var service = CreateService(new GitHubRepositoriesOptions { Owner = "octocat", OwnerType = "User" }, restClient);

		// Act
		var result = await service.GetRepositoriesAsync(TestContext.Current.CancellationToken);

		// Assert
		result.Repositories.Should().ContainSingle();
		result.ErrorMessage.Should().BeNull();
	}

	private static GitHubRepositoriesService CreateService(GitHubRepositoriesOptions options,
		IGitHubRestClient restClient) =>
		new(restClient, Options.Create(options));

	private sealed class FakeGitHubRestClient(Func<string, string?, object?> responder) : IGitHubRestClient
	{
		public Task<T?> TryGetAsync<T>(string path, string? token = null,
			CancellationToken cancellationToken = default) =>
			Task.FromResult((T?)responder(path, token));
	}
}
