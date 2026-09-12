// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     NotAuthorizedPageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Web.Components.Pages;

namespace Web.Tests.Bunit;

public class NotAuthorizedPageTests : BunitContext
{
	[Fact]
	public void NotAuthorizedPage_Rendered_ShowsMessageAndHomeLink()
	{
		// Arrange & Act
		var cut = Render<NotAuthorizedPage>();

		// Assert
		cut.Markup.Should().Contain("Not Authorized");
		cut.Find("[role='alert']").TextContent.Should().Contain("You are not authorized to access this resource.");

		var homeLink = cut.Find("a[href='/']");
		homeLink.TextContent.Should().Be("Go to Home");
	}
}
