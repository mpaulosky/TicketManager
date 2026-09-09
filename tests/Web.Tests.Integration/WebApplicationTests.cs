namespace Web.Tests.Integration;

public class WebApplicationTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public WebApplicationTests(WebApplicationFactory<Program> factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task Get_Root_ReturnsOk()
	{
		// Arrange
		using var client = _factory.CreateClient();

		// Act
		using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task Get_Root_UsesTailwindStylesheetInsteadOfBootstrap()
	{
		// Arrange
		using var client = _factory.CreateClient();

		// Act
		var markup = await client.GetStringAsync("/", TestContext.Current.CancellationToken);

		// Assert
		Assert.Contains("tailwind.", markup, StringComparison.Ordinal);
		Assert.DoesNotContain("bootstrap", markup, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Get_TailwindStylesheet_ImportsThemeStylesheet()
	{
		// Arrange
		using var client = _factory.CreateClient();

		// Act
		var css = await client.GetStringAsync("/tailwind.css", TestContext.Current.CancellationToken);

		// Assert
		Assert.Contains("@import url(\"/Style/Theme.css\");", css, StringComparison.Ordinal);
	}
}
