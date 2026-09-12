// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RedirectToNotAuthorizedPageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath.Should().Be("/not-authorized");
	}
}
