using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.Layout;

namespace Web.Tests.Bunit;

public class MainLayoutTests : BunitContext
{
	public MainLayoutTests()
	{
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		Services.AddSingleton<IHttpClientFactory>(new NotFoundHttpClientFactory());
	}

	[Fact]
	public void MainLayout_Rendered_ShowsBodyBetweenNavMenuAndFooter()
	{
		// Arrange
		var body = "<p>page body</p>";

		// Act
		var cut = Render<MainLayout>(parameters => parameters
			.Add(p => p.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, body))));

		// Assert
		Assert.Contains("page body", cut.Markup);
		Assert.NotNull(cut.Find("header.app-header"));
		Assert.NotNull(cut.Find("footer.app-footer"));
	}

	private sealed class NotFoundHttpClientFactory : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => new(new NotFoundHandler());
	}

	private sealed class NotFoundHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
	}
}
