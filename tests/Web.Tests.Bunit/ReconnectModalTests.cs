using Web.Components.Layout;

namespace Web.Tests.Bunit;

public class ReconnectModalTests : BunitContext
{
	[Fact]
	public void ReconnectModal_Rendered_ShowsReconnectDialogMarkup()
	{
		// Arrange & Act
		var cut = Render<ReconnectModal>();

		// Assert
		Assert.NotNull(cut.Find("#components-reconnect-modal"));
		Assert.Contains("Rejoining the server...", cut.Markup);
		Assert.Contains("Retry", cut.Markup);
	}
}
