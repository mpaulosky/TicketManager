// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppSettingsTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Unit
// =============================================

using Microsoft.Extensions.Configuration;

namespace Web.Tests.Unit;

public class AppSettingsTests
{
	[Fact]
	public void AppSettings_AllowedHosts_IsWildcard()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json")
			.Build();

		// Act
		var allowedHosts = configuration["AllowedHosts"];

		// Assert
		allowedHosts.Should().Be("*");
	}
}
