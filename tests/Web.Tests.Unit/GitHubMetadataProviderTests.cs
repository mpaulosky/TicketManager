// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitHubMetadataProviderTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Unit
// =============================================

using Web.Services;

namespace Web.Tests.Unit;

public class GitHubMetadataProviderTests
{
	[Theory]
	[InlineData("https://github.com/mpaulosky/TicketManager", "mpaulosky", "TicketManager")]
	[InlineData("https://github.com/mpaulosky/TicketManager.git", "mpaulosky", "TicketManager")]
	[InlineData("https://github.com/mpaulosky/TicketManager/", "mpaulosky", "TicketManager")]
	[InlineData("git@github.com:mpaulosky/TicketManager.git", "mpaulosky", "TicketManager")]
	[InlineData("ssh://git@github.com/mpaulosky/TicketManager.git", "mpaulosky", "TicketManager")]
	[InlineData("  https://github.com/mpaulosky/TicketManager  ", "mpaulosky", "TicketManager")]
	public void TryParseGitHubRepository_ValidRemoteUrl_ParsesOwnerAndRepo(string remoteUrl, string expectedOwner,
		string expectedRepo)
	{
		// Act
		var result = GitHubMetadataProvider.TryParseGitHubRepository(remoteUrl, out var owner, out var repo);

		// Assert
		result.Should().BeTrue();
		owner.Should().Be(expectedOwner);
		repo.Should().Be(expectedRepo);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("https://gitlab.com/mpaulosky/TicketManager")]
	[InlineData("https://github.com/mpaulosky")]
	[InlineData("https://github.com/")]
	public void TryParseGitHubRepository_InvalidOrIncompleteRemoteUrl_ReturnsFalse(string? remoteUrl)
	{
		// Act
		var result = GitHubMetadataProvider.TryParseGitHubRepository(remoteUrl, out var owner, out var repo);

		// Assert
		result.Should().BeFalse();
		owner.Should().Be(string.Empty);
		repo.Should().Be(string.Empty);
	}

	[Fact]
	public void Constructor_NullGitHubRestClient_ThrowsArgumentNullException()
	{
		// Act
		var act = () => new GitHubMetadataProvider(null!, new FakeGitCommandRunner((_, _) => null));

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Constructor_NullGitCommandRunner_ThrowsArgumentNullException()
	{
		// Act
		var act = () => new GitHubMetadataProvider(new FakeGitHubRestClient((_, _) => null), null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task GetMetadataAsync_NoOriginRemote_ReturnsNull()
	{
		// Arrange
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY_URL", null);
		Environment.SetEnvironmentVariable("REPOSITORY_URL", null);
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);

		var provider = new GitHubMetadataProvider(
			new FakeGitHubRestClient((_, _) =>
				throw new InvalidOperationException("GitHub should not be called without a resolved owner/repo.")),
			new FakeGitCommandRunner((_, _) => null));

		// Act
		var result = await provider.GetMetadataAsync(TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetMetadataAsync_ReleaseAndCommitAvailableFromGitHubApi_UsesApiValues()
	{
		// Arrange
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", "mpaulosky/TicketManager");

		try
		{
			var restClient = new FakeGitHubRestClient((path, _) =>
			{
				if (path.EndsWith("/releases/latest", StringComparison.Ordinal))
				{
					return new GitHubMetadataProvider.GitHubRelease { TagName = "v1.2.3" };
				}

				if (path.Contains("/commits/", StringComparison.Ordinal))
				{
					return new GitHubMetadataProvider.GitHubCommit { Sha = "abcdef1234567890" };
				}

				return null;
			});

			var gitRunner = new FakeGitCommandRunner((_, _) =>
				throw new InvalidOperationException("Local git should not be used when the API succeeds."));

			var provider = new GitHubMetadataProvider(restClient, gitRunner);

			// Act
			var result = await provider.GetMetadataAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.ReleaseTag.Should().Be("v1.2.3");
			result.LastCommit.Should().Be("abcdef1");
		}
		finally
		{
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);
		}
	}

	[Fact]
	public async Task GetMetadataAsync_GitHubApiHasNothing_FallsBackToLocalGit()
	{
		// Arrange
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", "mpaulosky/TicketManager");

		try
		{
			var restClient = new FakeGitHubRestClient((_, _) => null);
			var gitRunner = new FakeGitCommandRunner((workingDirectory, arguments) =>
			{
				workingDirectory.Should().NotBeEmpty();

				return arguments switch
				{
					["describe", "--tags", "--abbrev=0"] => "v0.9.0",
					["rev-parse", "--short", "HEAD"] => "1234567",
					_ => null,
				};
			});

			var provider = new GitHubMetadataProvider(restClient, gitRunner);

			// Act
			var result = await provider.GetMetadataAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.ReleaseTag.Should().Be("v0.9.0");
			result.LastCommit.Should().Be("1234567");
		}
		finally
		{
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);
		}
	}

	[Fact]
	public async Task GetMetadataAsync_NothingAvailableAnywhere_ReturnsFallbackLabels()
	{
		// Arrange
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", "mpaulosky/TicketManager");

		try
		{
			var provider = new GitHubMetadataProvider(
				new FakeGitHubRestClient((_, _) => null),
				new FakeGitCommandRunner((_, _) => null));

			// Act
			var result = await provider.GetMetadataAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.ReleaseTag.Should().Be("no release");
			result.LastCommit.Should().Be("unknown");
		}
		finally
		{
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);
		}
	}

	[Fact]
	public async Task GetMetadataAsync_LocalGitCommandThrowsWhileResolvingOrigin_ReturnsNull()
	{
		// Arrange
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY_URL", null);
		Environment.SetEnvironmentVariable("REPOSITORY_URL", null);
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);

		var provider = new GitHubMetadataProvider(
			new FakeGitHubRestClient((_, _) =>
				throw new InvalidOperationException("GitHub should not be called without a resolved owner/repo.")),
			new FakeGitCommandRunner((_, arguments) => arguments is ["remote", "get-url", "origin"]
				? throw new InvalidOperationException("no such remote 'origin'")
				: null));

		// Act
		var result = await provider.GetMetadataAsync(TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetMetadataAsync_LocalReleaseTagLookupThrows_FallsBackToNoRelease()
	{
		// Arrange
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", "mpaulosky/TicketManager");

		try
		{
			var restClient = new FakeGitHubRestClient((_, _) => null);
			var gitRunner = new FakeGitCommandRunner((_, arguments) => arguments switch
			{
				["describe", "--tags", "--abbrev=0"] => throw new InvalidOperationException("no tags found"),
				["rev-parse", "--short", "HEAD"] => "1234567",
				_ => null,
			});

			var provider = new GitHubMetadataProvider(restClient, gitRunner);

			// Act
			var result = await provider.GetMetadataAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.ReleaseTag.Should().Be("no release");
			result.LastCommit.Should().Be("1234567");
		}
		finally
		{
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);
		}
	}

	[Fact]
	public async Task GetMetadataAsync_LocalLastCommitLookupThrows_FallsBackToUnknown()
	{
		// Arrange
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", "mpaulosky/TicketManager");

		try
		{
			var restClient = new FakeGitHubRestClient((_, _) => null);
			var gitRunner = new FakeGitCommandRunner((_, arguments) => arguments switch
			{
				["describe", "--tags", "--abbrev=0"] => "v0.9.0",
				["rev-parse", "--short", "HEAD"] => throw new InvalidOperationException("not a git repository"),
				_ => null,
			});

			var provider = new GitHubMetadataProvider(restClient, gitRunner);

			// Act
			var result = await provider.GetMetadataAsync(TestContext.Current.CancellationToken);

			// Assert
			result.Should().NotBeNull();
			result.ReleaseTag.Should().Be("v0.9.0");
			result.LastCommit.Should().Be("unknown");
		}
		finally
		{
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY", null);
		}
	}

	private sealed class FakeGitHubRestClient(Func<string, string?, object?> responder) : IGitHubRestClient
	{
		public Task<T?> TryGetAsync<T>(string path, string? token = null,
			CancellationToken cancellationToken = default) =>
			Task.FromResult((T?)responder(path, token));
	}

	private sealed class FakeGitCommandRunner(Func<string, string[], string?> responder) : IGitCommandRunner
	{
		public Task<string?> RunAsync(string workingDirectory, params string[] arguments) =>
			Task.FromResult(responder(workingDirectory, arguments));
	}
}
