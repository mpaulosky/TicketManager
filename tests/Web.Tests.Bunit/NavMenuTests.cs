// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     NavMenuTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Web.Components.Layout;

namespace Web.Tests.Bunit;

public class NavMenuTests : BunitContext
{
	public NavMenuTests()
	{
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
	}

	[Fact]
	public void NavMenu_Rendered_ShowsBrandAndNavigationLinks()
	{
		// Arrange & Act
		var cut = Render<NavMenu>();

		// Assert
		cut.Markup.Should().Contain("Ticket Manager");
		cut.Find("a[href='/']").Should().NotBeNull();
		cut.Find("a[href='/repositories']").Should().NotBeNull();
	}

	[Fact]
	public void NavMenu_Rendered_ShowsLightThemeToggleByDefault()
	{
		// Arrange & Act
		var cut = Render<NavMenu>();

		// Assert
		var toggle = cut.Find("button.theme-toggle");
		toggle.GetAttribute("aria-label").Should().Be("Switch to dark theme");
	}

	[Fact]
	public void NavMenu_ToggleThemeClicked_SwitchesToDarkTheme()
	{
		// Arrange
		var cut = Render<NavMenu>();
		var toggle = cut.Find("button.theme-toggle");

		// Act
		toggle.Click();

		// Assert
		toggle = cut.Find("button.theme-toggle");
		toggle.GetAttribute("aria-label").Should().Be("Switch to light theme");
		toggle.TextContent.Should().Contain("☀️");
	}
}
