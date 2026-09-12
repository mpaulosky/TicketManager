// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ReconnectModalTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		cut.Find("#components-reconnect-modal").Should().NotBeNull();
		cut.Markup.Should().Contain("Rejoining the server...");
		cut.Markup.Should().Contain("Retry");
	}
}
