// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     WebApplicationTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Integration
// =============================================

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
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task Get_Root_UsesTailwindStylesheetInsteadOfBootstrap()
	{
		// Arrange
		using var client = _factory.CreateClient();

		// Act
		var markup = await client.GetStringAsync("/", TestContext.Current.CancellationToken);

		// Assert
		markup.Should().Contain("css/app");
		markup.Should().NotContainEquivalentOf("bootstrap");
	}

	[Fact]
	public async Task Get_AppStylesheet_ReturnsOk()
	{
		// Arrange
		using var client = _factory.CreateClient();

		// Act
		using var response = await client.GetAsync("/css/app.css", TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task Get_UnknownRoute_ReturnsNotFoundPageContent()
	{
		// Arrange
		using var client = _factory.CreateClient();

		// Act
		using var response = await client.GetAsync("/this-route-does-not-exist",
			TestContext.Current.CancellationToken);
		var markup = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		markup.Should().Contain("Sorry, the content you are looking for does not exist.");
	}

	[Fact]
	public async Task Get_RepositoriesRoute_ReturnsOk()
	{
		// Arrange
		using var client = _factory.CreateClient();

		// Act
		using var response = await client.GetAsync("/repositories", TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}
}
