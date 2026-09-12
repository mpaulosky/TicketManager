using Microsoft.Extensions.DependencyInjection;
using Web.Components;
using Web.Services;

namespace Web.Tests.Bunit;

public class RoutesTests : BunitContext
{
	public RoutesTests()
	{
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		Services.AddSingleton<IGitHubRestClient>(new NullGitHubRestClient());
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

	private sealed class NullGitHubRestClient : IGitHubRestClient
	{
		public Task<T?> TryGetAsync<T>(string path, string? token = null,
			CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
	}
}
