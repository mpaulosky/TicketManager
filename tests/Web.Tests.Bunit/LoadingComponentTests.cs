using Web.Components.Shared;

namespace Web.Tests.Bunit;

public class LoadingComponentTests : BunitContext
{
	[Fact]
	public void LoadingComponent_Rendered_ShowsLoadingMessage()
	{
		// Arrange & Act
		var cut = Render<LoadingComponent>();

		// Assert
		Assert.Contains("Loading...", cut.Markup);
	}
}
