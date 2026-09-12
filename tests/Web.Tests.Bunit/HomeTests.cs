// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     HomeTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Web.Components.Pages;

namespace Web.Tests.Bunit;

public class HomeTests : BunitContext
{
	[Fact]
	public void Home_Rendered_ShowsDefaultTemplateMarkup()
	{
		// Arrange & Act
		var cut = Render<Home>();

		// Assert
		cut.Find("h1").TextContent.Should().Be("Articles");
		cut.Markup.Should().Contain("Build a durable foundation for modern .NET application work");
	}
}
