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
		Assert.Equal("Ticket Manager", await Page.TitleAsync());
		Assert.Contains("Articles", await Page.ContentAsync());
	}
}
