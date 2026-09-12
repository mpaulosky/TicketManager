using Web.Services;

namespace Web.Tests.Unit;

public class GitHubRepositoriesResultTests
{
	[Fact]
	public void Empty_NoErrorMessage_ReturnsResultWithNoRepositoriesAndNullError()
	{
		// Act
		var result = GitHubRepositoriesResult.Empty(isAuthenticated: true);

		// Assert
		Assert.True(result.IsAuthenticated);
		Assert.Empty(result.Repositories);
		Assert.Null(result.ErrorMessage);
	}

	[Fact]
	public void Empty_WithErrorMessage_ReturnsResultCarryingTheErrorMessage()
	{
		// Act
		var result = GitHubRepositoriesResult.Empty(isAuthenticated: false, errorMessage: "boom");

		// Assert
		Assert.False(result.IsAuthenticated);
		Assert.Empty(result.Repositories);
		Assert.Equal("boom", result.ErrorMessage);
	}
}
