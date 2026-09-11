using Web.Services;

namespace Web.Tests.Unit;

public class GitHubProjectsOptionsTests
{
	[Fact]
	public void OwnerType_Default_IsUser()
	{
		// Arrange & Act
		var options = new GitHubProjectsOptions();

		// Assert
		Assert.Equal("User", options.OwnerType);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void HasToken_NoToken_ReturnsFalse(string? token)
	{
		// Arrange
		var options = new GitHubProjectsOptions { Token = token };

		// Act & Assert
		Assert.False(options.HasToken);
	}

	[Fact]
	public void HasToken_TokenConfigured_ReturnsTrue()
	{
		// Arrange
		var options = new GitHubProjectsOptions { Token = "gh-token" };

		// Act & Assert
		Assert.True(options.HasToken);
	}
}
