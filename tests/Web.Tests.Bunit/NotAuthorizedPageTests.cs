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
		Assert.Contains("Not Authorized", cut.Markup);
		Assert.Contains("You are not authorized to access this resource.", cut.Find("[role='alert']").TextContent);

		var homeLink = cut.Find("a[href='/']");
		Assert.Equal("Go to Home", homeLink.TextContent);
	}
}
