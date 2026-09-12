// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitHubRepositoriesTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		cut.Find("[data-testid='repo-card'] h2").TextContent.Trim().Should().Be("repo-a");

		var issueLink = cut.Find("a[href='https://github.com/octocat/repo-a/issues/1']");
		issueLink.TextContent.Should().Contain("Fix the thing");

		var pullRequestLink = cut.Find("a[href='https://github.com/octocat/repo-a/pull/2']");
		pullRequestLink.TextContent.Should().Contain("Add the feature");
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
		cut.Markup.Should().Contain("Unauthenticated");
	}

	[Fact]
	public void GitHubRepositories_NoErrorAndNoRepositories_ShowsNoRepositoriesFoundMessage()
	{
		// Arrange
		var result = GitHubRepositoriesResult.Empty(isAuthenticated: true);
		Services.AddSingleton<IGitHubRepositoriesService>(new FakeGitHubRepositoriesService(result));

		// Act
		var cut = Render<GitHubRepositories>();

		// Assert
		cut.Markup.Should().Contain("No repositories were found for the configured GitHub owner.");
	}

	[Fact]
	public void GitHubRepositories_RepositoryWithNoIssuesOrPullRequests_ShowsEmptyStateCopy()
	{
		// Arrange
		var result = new GitHubRepositoriesResult(
			IsAuthenticated: true,
			Repositories: [new GitHubRepositoryStatus("repo-a", "https://github.com/octocat/repo-a", [], [])],
			ErrorMessage: null);

		Services.AddSingleton<IGitHubRepositoriesService>(new FakeGitHubRepositoriesService(result));

		// Act
		var cut = Render<GitHubRepositories>();

		// Assert
		cut.Markup.Should().Contain("No open issues.");
		cut.Markup.Should().Contain("No open pull requests.");
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
		cut.Markup.Should().Contain("No GitHub owner is configured.");
	}

	private sealed class FakeGitHubRepositoriesService(GitHubRepositoriesResult result) : IGitHubRepositoriesService
	{
		public Task<GitHubRepositoriesResult> GetRepositoriesAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(result);
	}
}
