// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     FooterTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Bunit
// =============================================

using Microsoft.Extensions.DependencyInjection;

using Web.Components.Layout;
using Web.Services;

namespace Web.Tests.Bunit;

public class FooterTests : BunitContext
{
	public FooterTests()
	{
		// Prevent the footer's best-effort GitHub metadata lookup from making real network/process calls.
		Services.AddSingleton<IGitHubMetadataProvider>(new NullGitHubMetadataProvider());
	}

	[Fact]
	public void Footer_Rendered_ShowsCopyrightWithCurrentYearAndCompanyName()
	{
		// Arrange & Act
		var cut = Render<Footer>();

		// Assert
		cut.Markup.Should().Contain($"© {DateTime.UtcNow.Year} Ticket Manager");
		cut.Markup.Should().Contain("from mpaulosky.org");
	}

	[Fact]
	public void Footer_Rendered_LinksToTheGitHubRepository()
	{
		// Arrange & Act
		var cut = Render<Footer>();

		// Assert
		var releaseLink = cut.Find("a.app-footer-label");
		releaseLink.GetAttribute("href").Should().StartWith("https://github.com/mpaulosky/TicketManager");

		var commitLink = cut.FindAll("a.app-footer-value")[1];
		commitLink.GetAttribute("href").Should().StartWith("https://github.com/mpaulosky/TicketManager");
	}

	private sealed class NullGitHubMetadataProvider : IGitHubMetadataProvider
	{
		public Task<GitHubMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<GitHubMetadata?>(null);
	}
}
