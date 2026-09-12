// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RoutesTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

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
		Services.AddSingleton<IGitHubMetadataProvider>(new NullGitHubMetadataProvider());
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
		cut.Markup.Should().Contain("Sorry, the content you are looking for does not exist.");
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
		cut.Markup.Should().Contain("You are not authorized to access this resource.");
	}

	private sealed class NullGitHubMetadataProvider : IGitHubMetadataProvider
	{
		public Task<GitHubMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<GitHubMetadata?>(null);
	}
}
