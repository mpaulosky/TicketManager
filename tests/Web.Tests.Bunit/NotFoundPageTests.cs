// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     NotFoundPageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Web.Components.Pages;

namespace Web.Tests.Bunit;

public class NotFoundPageTests : BunitContext
{
	[Fact]
	public void NotFound_Rendered_ShowsNotFoundMessage()
	{
		// Arrange & Act
		var cut = Render<NotFound>();

		// Assert
		cut.Find("h3").TextContent.Should().Be("Not Found");
		cut.Markup.Should().Contain("Sorry, the content you are looking for does not exist.");
	}
}
