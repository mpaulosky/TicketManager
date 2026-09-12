// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LoadingComponentTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		cut.Markup.Should().Contain("Loading...");
	}
}
