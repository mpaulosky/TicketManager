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
		Assert.Equal("Not Found", cut.Find("h3").TextContent);
		Assert.Contains("Sorry, the content you are looking for does not exist.", cut.Markup);
	}
}
