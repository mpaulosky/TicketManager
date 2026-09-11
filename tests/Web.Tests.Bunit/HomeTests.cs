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
		Assert.Equal("Articles", cut.Find("h1").TextContent);
		Assert.Contains("Build a durable foundation for modern .NET application work", cut.Markup);
	}
}
