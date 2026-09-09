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
		Assert.Equal("Hello, world!", cut.Find("h1").TextContent);
		Assert.Contains("Welcome to your new app.", cut.Markup);
	}
}
