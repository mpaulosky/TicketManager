using System.Diagnostics.CodeAnalysis;

namespace Web.Services;

/// <summary>
/// Configuration for the GitHub repository dashboard.
/// Bound from the "GitHub" configuration section (appsettings, user-secrets, or environment variables).
/// The <see cref="Token" /> should never be committed to source control; supply it via user-secrets
/// (`dotnet user-secrets set GitHub:Token "..."`) or the GITHUB_TOKEN environment variable.
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification = "Bound via IOptions and consumed by the Web project and its tests.")]
public sealed class GitHubRepositoriesOptions
{
	public const string SectionName = "GitHub";

	/// <summary>
	/// The GitHub username or organization login whose repositories should be listed.
	/// </summary>
	public string? Owner { get; set; }

	/// <summary>
	/// Whether <see cref="Owner" /> refers to a "User" or an "Org". Defaults to "User" and falls back
	/// to "Org" automatically when the user lookup returns a 404.
	/// </summary>
	public string OwnerType { get; set; } = "User";

	/// <summary>
	/// A GitHub Personal Access Token used to authenticate requests to the GitHub REST API. When not
	/// supplied (via configuration or the GITHUB_TOKEN environment variable), requests are made
	/// unauthenticated against public repositories only and are subject to GitHub's lower rate limits.
	/// </summary>
	public string? Token { get; set; }

	public bool HasToken => !string.IsNullOrWhiteSpace(Token);
}
