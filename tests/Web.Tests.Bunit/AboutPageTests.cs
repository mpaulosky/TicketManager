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
		Assert.Equal("About TicketManager", cut.Find("h1").TextContent);
	}
}
