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
		Assert.Contains($"© {DateTime.UtcNow.Year} Ticket Manager", cut.Markup);
		Assert.Contains("from mpaulosky.org", cut.Markup);
	}

	[Fact]
	public void Footer_Rendered_LinksToTheGitHubRepository()
	{
		// Arrange & Act
		var cut = Render<Footer>();

		// Assert
		var releaseLink = cut.Find("a.app-footer-label");
		Assert.StartsWith("https://github.com/mpaulosky/TicketManager", releaseLink.GetAttribute("href"));

		var commitLink = cut.FindAll("a.app-footer-value")[1];
		Assert.StartsWith("https://github.com/mpaulosky/TicketManager", commitLink.GetAttribute("href"));
	}

	private sealed class NullGitHubMetadataProvider : IGitHubMetadataProvider
	{
		public Task<GitHubMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<GitHubMetadata?>(null);
	}
}
