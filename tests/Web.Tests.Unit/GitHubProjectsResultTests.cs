using Web.Services;

namespace Web.Tests.Unit;

public class GitHubProjectsResultTests
{
	[Fact]
	public void Empty_NoErrorMessage_ReturnsResultWithNoRepositoriesAndNullError()
	{
		// Act
		var result = GitHubProjectsResult.Empty(isAuthenticated: true);

		// Assert
		Assert.True(result.IsAuthenticated);
		Assert.Empty(result.Repositories);
		Assert.Null(result.ErrorMessage);
	}

	[Fact]
	public void Empty_WithErrorMessage_ReturnsResultCarryingTheErrorMessage()
	{
		// Act
		var result = GitHubProjectsResult.Empty(isAuthenticated: false, errorMessage: "boom");

		// Assert
		Assert.False(result.IsAuthenticated);
		Assert.Empty(result.Repositories);
		Assert.Equal("boom", result.ErrorMessage);
	}
}
