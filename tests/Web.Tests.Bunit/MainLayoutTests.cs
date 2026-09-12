// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MainLayoutTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		cut.Markup.Should().Contain("page body");
		cut.Find("header.app-header").Should().NotBeNull();
		cut.Find("footer.app-footer").Should().NotBeNull();
	}

	private sealed class NullGitHubMetadataProvider : IGitHubMetadataProvider
	{
		public Task<GitHubMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<GitHubMetadata?>(null);
	}
}
