// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RedirectToLoginPageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath.Should().Be("/Account/Login");
	}
}
