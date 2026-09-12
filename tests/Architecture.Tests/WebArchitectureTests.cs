// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     WebArchitectureTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Architecture.Tests
// =============================================

using NetArchTest.Rules;

namespace Architecture.Tests;

public class WebArchitectureTests
{
	[Fact]
	public void Web_DoesNotReferenceSqlClientAssemblies()
	{
		// Arrange
		var webAssembly = typeof(Web.Components.App).Assembly;

		// Act
		var result = Types.InAssembly(webAssembly)
			.Should()
			.NotHaveDependencyOnAny("System.Data.SqlClient", "Microsoft.Data.SqlClient")
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
	}
}
