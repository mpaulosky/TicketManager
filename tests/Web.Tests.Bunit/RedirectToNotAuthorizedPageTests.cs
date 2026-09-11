using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.Pages;

namespace Web.Tests.Bunit;

public class RedirectToNotAuthorizedPageTests : BunitContext
{
	[Fact]
	public void RedirectToNotAuthorizedPage_Rendered_NavigatesToNotAuthorized()
	{
		// Act
		Render<RedirectToNotAuthorizedPage>();

		// Assert
		Assert.Equal("/not-authorized", new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath);
	}
}
