// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitHubRepositoriesResultTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Unit
// =============================================

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
		result.IsAuthenticated.Should().BeTrue();
		result.Repositories.Should().BeEmpty();
		result.ErrorMessage.Should().BeNull();
	}

	[Fact]
	public void Empty_WithErrorMessage_ReturnsResultCarryingTheErrorMessage()
	{
		// Act
		var result = GitHubRepositoriesResult.Empty(isAuthenticated: false, errorMessage: "boom");

		// Assert
		result.IsAuthenticated.Should().BeFalse();
		result.Repositories.Should().BeEmpty();
		result.ErrorMessage.Should().Be("boom");
	}
}
