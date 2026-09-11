using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.Pages;

namespace Web.Tests.Bunit;

public class RedirectToLoginPageTests : BunitContext
{
	[Fact]
	public void RedirectToLoginPage_Rendered_NavigatesToAccountLoginWithForceLoad()
	{
		// Act
		Render<RedirectToLoginPage>();

		// Assert
		Assert.Equal("/Account/Login", new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath);
	}
}
