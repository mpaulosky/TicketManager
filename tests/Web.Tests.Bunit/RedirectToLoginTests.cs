// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RedirectToLoginTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/Account/Login");
		Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("returnUrl=");
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
		Services.GetRequiredService<NavigationManager>().Uri.Should().Be(initialUri);
	}
}
