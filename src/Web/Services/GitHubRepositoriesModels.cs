// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitHubRepositoriesModels.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web
// =============================================

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Web.Services;

/// <summary>
///   A single open issue or pull request, ready for display/linking in the UI.
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification = "Consumed by the GitHub Repositories Razor page and its component tests.")]
public sealed record GitHubIssueSummary(int Number, string Title, string Url);

/// <summary>
///   The open issues and pull requests for a single repository.
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification = "Consumed by the GitHub Repositories Razor page and its component tests.")]
public sealed record GitHubRepositoryStatus(
	string Name,
	string Url,
	IReadOnlyList<GitHubIssueSummary> Issues,
	IReadOnlyList<GitHubIssueSummary> PullRequests);

/// <summary>
///   The overall result of loading the GitHub repository dashboard, including any degraded-mode or error
///   state so the UI can render a friendly message instead of failing outright.
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification = "Consumed by the GitHub Repositories Razor page and its component tests.")]
public sealed record GitHubRepositoriesResult(
	bool IsAuthenticated,
	IReadOnlyList<GitHubRepositoryStatus> Repositories,
	string? ErrorMessage)
{
	public static GitHubRepositoriesResult Empty(bool isAuthenticated, string? errorMessage = null) =>
		new(isAuthenticated, [], errorMessage);
}

internal sealed class GitHubRepositoryDto
{
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = string.Empty;

	public bool Archived { get; set; }

	public bool Fork { get; set; }
}

internal sealed class GitHubIssueDto
{
	public int Number { get; set; }

	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = string.Empty;

	/// <summary>
	///   GitHub's issues API also returns pull requests. When this is non-null, the "issue" is really a PR.
	/// </summary>
	[JsonPropertyName("pull_request")]
	public object? PullRequest { get; set; }
}
