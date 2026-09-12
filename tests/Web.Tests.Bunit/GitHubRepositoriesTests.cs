using Microsoft.Extensions.DependencyInjection;
using Web.Components.Pages;
using Web.Services;

namespace Web.Tests.Bunit;

public class GitHubRepositoriesTests : BunitContext
{
	[Fact]
	public void GitHubRepositories_Rendered_ShowsOneCardPerRepositoryWithIssuesAndPullRequests()
	{
		// Arrange
		var result = new GitHubRepositoriesResult(
			IsAuthenticated: true,
			Repositories:
			[
				new GitHubRepositoryStatus(
					"repo-a",
					"https://github.com/octocat/repo-a",
					[new GitHubIssueSummary(1, "Fix the thing", "https://github.com/octocat/repo-a/issues/1")],
					[new GitHubIssueSummary(2, "Add the feature", "https://github.com/octocat/repo-a/pull/2")]),
			],
			ErrorMessage: null);

		Services.AddSingleton<IGitHubRepositoriesService>(new FakeGitHubRepositoriesService(result));

		// Act
		var cut = Render<GitHubRepositories>();

		// Assert
		Assert.Equal("repo-a", cut.Find("[data-testid='repo-card'] h2").TextContent.Trim());

		var issueLink = cut.Find("a[href='https://github.com/octocat/repo-a/issues/1']");
		Assert.Contains("Fix the thing", issueLink.TextContent);

		var pullRequestLink = cut.Find("a[href='https://github.com/octocat/repo-a/pull/2']");
		Assert.Contains("Add the feature", pullRequestLink.TextContent);
	}

	[Fact]
	public void GitHubRepositories_NoTokenConfigured_ShowsUnauthenticatedBadge()
	{
		// Arrange
		var result = GitHubRepositoriesResult.Empty(isAuthenticated: false);
		Services.AddSingleton<IGitHubRepositoriesService>(new FakeGitHubRepositoriesService(result));

		// Act
		var cut = Render<GitHubRepositories>();

		// Assert
		Assert.Contains("Unauthenticated", cut.Markup);
	}

	[Fact]
	public void GitHubRepositories_ErrorResult_ShowsFriendlyErrorMessage()
	{
		// Arrange
		var result = GitHubRepositoriesResult.Empty(isAuthenticated: false, errorMessage: "No GitHub owner is configured.");
		Services.AddSingleton<IGitHubRepositoriesService>(new FakeGitHubRepositoriesService(result));

		// Act
		var cut = Render<GitHubRepositories>();

		// Assert
		Assert.Contains("No GitHub owner is configured.", cut.Markup);
	}

	private sealed class FakeGitHubRepositoriesService(GitHubRepositoriesResult result) : IGitHubRepositoriesService
	{
		public Task<GitHubRepositoriesResult> GetRepositoriesAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(result);
	}
}
