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
		Assert.True(result);
		Assert.Equal(expectedOwner, owner);
		Assert.Equal(expectedRepo, repo);
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
		Assert.False(result);
		Assert.Equal(string.Empty, owner);
		Assert.Equal(string.Empty, repo);
	}

	[Fact]
	public void Constructor_NullGitHubRestClient_ThrowsArgumentNullException()
	{
		// Act
		var act = () => new GitHubMetadataProvider(null!, new FakeGitCommandRunner((_, _) => null));

		// Assert
		Assert.Throws<ArgumentNullException>(act);
	}

	[Fact]
	public void Constructor_NullGitCommandRunner_ThrowsArgumentNullException()
	{
		// Act
		var act = () => new GitHubMetadataProvider(new FakeGitHubRestClient((_, _) => null), null!);

		// Assert
		Assert.Throws<ArgumentNullException>(act);
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
		Assert.Null(result);
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
			Assert.NotNull(result);
			Assert.Equal("v1.2.3", result.ReleaseTag);
			Assert.Equal("abcdef1", result.LastCommit);
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
				Assert.NotEmpty(workingDirectory);

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
			Assert.NotNull(result);
			Assert.Equal("v0.9.0", result.ReleaseTag);
			Assert.Equal("1234567", result.LastCommit);
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
			Assert.NotNull(result);
			Assert.Equal("no release", result.ReleaseTag);
			Assert.Equal("unknown", result.LastCommit);
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
