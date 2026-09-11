using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Web.Components;

namespace Web.Tests.Bunit;

public class RoutesTests : BunitContext
{
	public RoutesTests()
	{
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		Services.AddSingleton<IHttpClientFactory>(new NotFoundHttpClientFactory());
	}

	[Fact]
	public void Routes_UnknownRoute_RendersNotFoundPage()
	{
		// Arrange
		Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
			.NavigateTo("/this-route-does-not-exist");

		// Act
		var cut = Render<Routes>();

		// Assert
		Assert.Contains("Sorry, the content you are looking for does not exist.", cut.Markup);
	}

	[Fact]
	public void Routes_KnownRoute_RendersMatchingPage()
	{
		// Arrange
		Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
			.NavigateTo("/not-authorized");

		// Act
		var cut = Render<Routes>();

		// Assert
		Assert.Contains("You are not authorized to access this resource.", cut.Markup);
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
