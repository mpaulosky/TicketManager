using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Web.Components.Layout;
using Web.Services;

namespace Web.Tests.Bunit;

public class MainLayoutTests : BunitContext
{
	public MainLayoutTests()
	{
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		Services.AddSingleton<IGitHubMetadataProvider>(new NullGitHubMetadataProvider());
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

	private sealed class NullGitHubMetadataProvider : IGitHubMetadataProvider
	{
		public Task<GitHubMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<GitHubMetadata?>(null);
	}
}
