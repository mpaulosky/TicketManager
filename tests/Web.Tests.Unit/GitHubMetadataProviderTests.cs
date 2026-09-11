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
	public async Task GetMetadataAsync_NullHttpClient_ThrowsArgumentNullException()
	{
		// Act
		var act = () => GitHubMetadataProvider.GetMetadataAsync(null!, TestContext.Current.CancellationToken);

		// Assert
		await Assert.ThrowsAsync<ArgumentNullException>(act);
	}
}
