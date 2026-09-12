// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AboutPageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Web.Components.Pages;

namespace Web.Tests.Bunit;

public class AboutPageTests : BunitContext
{
	[Fact]
	public void About_Rendered_ShowsAboutHeading()
	{
		// Arrange & Act
		var cut = Render<About>();

		// Assert
		cut.Find("h1").TextContent.Should().Be("About TicketManager");
	}
}
