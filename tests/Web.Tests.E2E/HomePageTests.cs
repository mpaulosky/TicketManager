// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     HomePageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.E2E
// =============================================

namespace Web.Tests.E2E;

public class HomePageTests : PageTest, IClassFixture<WebFactory>
{
	private readonly WebFactory _factory;

	public HomePageTests(WebFactory factory)
	{
		_factory = factory;
		_factory.EnsureStarted();
	}

	public override BrowserNewContextOptions ContextOptions()
	{
		var options = base.ContextOptions();
		options.BaseURL = _factory.ServerAddress;
		return options;
	}

	[Fact]
	public async Task Get_Root_ShowsDefaultTemplateTitleAndContent()
	{
		// Act
		await Page.GotoAsync("/");

		// Assert
		(await Page.TitleAsync()).Should().Be("Ticket Manager");
		(await Page.ContentAsync()).Should().Contain("Articles");
	}
}
