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
		Assert.Contains("Ticket Manager", cut.Markup);
		Assert.NotNull(cut.Find("a[href='/']"));
		Assert.NotNull(cut.Find("a[href='/projects']"));
	}

	[Fact]
	public void NavMenu_Rendered_ShowsLightThemeToggleByDefault()
	{
		// Arrange & Act
		var cut = Render<NavMenu>();

		// Assert
		var toggle = cut.Find("button.theme-toggle");
		Assert.Equal("Switch to dark theme", toggle.GetAttribute("aria-label"));
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
		Assert.Equal("Switch to light theme", toggle.GetAttribute("aria-label"));
		Assert.Contains("☀️", toggle.TextContent);
	}
}
