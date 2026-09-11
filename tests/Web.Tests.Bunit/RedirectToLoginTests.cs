using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.Shared;

namespace Web.Tests.Bunit;

public class RedirectToLoginTests : BunitContext
{
	[Fact]
	public void RedirectToLogin_AuthenticationConfigured_NavigatesToAccountLoginWithReturnUrl()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Auth0:Domain"] = "domain", ["Auth0:ClientId"] = "clientId", ["Auth0:ClientSecret"] = "clientSecret",
			})
			.Build();
		Services.AddSingleton<IConfiguration>(configuration);

		// Act
		Render<RedirectToLogin>();

		// Assert
		Assert.Contains("/Account/Login", Services.GetRequiredService<NavigationManager>().Uri);
		Assert.Contains("returnUrl=", Services.GetRequiredService<NavigationManager>().Uri);
	}

	[Fact]
	public void RedirectToLogin_AuthenticationNotConfigured_DoesNotNavigate()
	{
		// Arrange
		var configuration = new ConfigurationBuilder().Build();
		Services.AddSingleton<IConfiguration>(configuration);
		var initialUri = Services.GetRequiredService<NavigationManager>().Uri;

		// Act
		Render<RedirectToLogin>();

		// Assert
		Assert.Equal(initialUri, Services.GetRequiredService<NavigationManager>().Uri);
	}
}
