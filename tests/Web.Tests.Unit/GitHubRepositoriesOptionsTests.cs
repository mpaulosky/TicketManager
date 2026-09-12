// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitHubRepositoriesOptionsTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Unit
// =============================================

using Web.Services;

namespace Web.Tests.Unit;

public class GitHubRepositoriesOptionsTests
{
	[Fact]
	public void OwnerType_Default_IsUser()
	{
		// Arrange & Act
		var options = new GitHubRepositoriesOptions();

		// Assert
		options.OwnerType.Should().Be("User");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void HasToken_NoToken_ReturnsFalse(string? token)
	{
		// Arrange
		var options = new GitHubRepositoriesOptions { Token = token };

		// Act & Assert
		options.HasToken.Should().BeFalse();
	}

	[Fact]
	public void HasToken_TokenConfigured_ReturnsTrue()
	{
		// Arrange
		var options = new GitHubRepositoriesOptions { Token = "gh-token" };

		// Act & Assert
		options.HasToken.Should().BeTrue();
	}
}
