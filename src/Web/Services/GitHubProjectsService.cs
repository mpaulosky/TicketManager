using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Web.Services;

[SuppressMessage("Design", "CA1515",
	Justification = "Injected into the GitHub Projects Razor page and consumed by Web tests.")]
public interface IGitHubProjectsService
{
	Task<GitHubProjectsResult> GetProjectsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Loads a per-repository snapshot of open issues and pull requests for a configured GitHub
/// user/organization. Owns the orgs-vs-users fallback policy and the open-issues/open-pull-requests
/// split; the actual GitHub HTTP calls go through <see cref="IGitHubRestClient" />.
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification = "Injected into the GitHub Projects Razor page and consumed by Web tests.")]
public sealed class GitHubProjectsService : IGitHubProjectsService
{
	private readonly IGitHubRestClient _gitHubRestClient;
	private readonly GitHubProjectsOptions _options;

	public GitHubProjectsService(IGitHubRestClient gitHubRestClient, IOptions<GitHubProjectsOptions> options)
	{
		ArgumentNullException.ThrowIfNull(gitHubRestClient);
		ArgumentNullException.ThrowIfNull(options);

		_gitHubRestClient = gitHubRestClient;
		_options = options.Value;
	}

	public async Task<GitHubProjectsResult> GetProjectsAsync(CancellationToken cancellationToken = default)
	{
		var owner = _options.Owner?.Trim();
		var isAuthenticated = _options.HasToken;

		if (string.IsNullOrWhiteSpace(owner))
		{
			return GitHubProjectsResult.Empty(isAuthenticated,
				"No GitHub owner is configured. Set the \"GitHub:Owner\" setting to a GitHub username or organization.");
		}

		var repositories = await GetRepositoriesAsync(owner, cancellationToken).ConfigureAwait(false);
		if (repositories is null)
		{
			return GitHubProjectsResult.Empty(isAuthenticated,
				$"Could not find a GitHub user or organization named \"{owner}\".");
		}

		var statuses = await Task.WhenAll(repositories.Select(repo => GetRepositoryStatusAsync(owner, repo, cancellationToken)))
			.ConfigureAwait(false);

		return new GitHubProjectsResult(isAuthenticated, statuses.OrderBy(status => status.Name,
			StringComparer.OrdinalIgnoreCase).ToList(), null);
	}

	private async Task<IReadOnlyList<GitHubRepositoryDto>?> GetRepositoriesAsync(string owner,
		CancellationToken cancellationToken)
	{
		var preferredSegment = string.Equals(_options.OwnerType, "Org", StringComparison.OrdinalIgnoreCase)
			? "orgs"
			: "users";
		var fallbackSegment = preferredSegment == "orgs" ? "users" : "orgs";

		var repositories = await GetRepositoriesForSegmentAsync(preferredSegment, owner, cancellationToken)
			.ConfigureAwait(false);

		return repositories ?? await GetRepositoriesForSegmentAsync(fallbackSegment, owner, cancellationToken)
			.ConfigureAwait(false);
	}

	private async Task<IReadOnlyList<GitHubRepositoryDto>?> GetRepositoriesForSegmentAsync(string ownerSegment,
		string owner, CancellationToken cancellationToken)
	{
		var repositories = await _gitHubRestClient
			.TryGetAsync<List<GitHubRepositoryDto>>($"{ownerSegment}/{owner}/repos?per_page=100&sort=updated",
				_options.Token, cancellationToken)
			.ConfigureAwait(false);

		return repositories?.Where(repo => !repo.Fork && !repo.Archived).ToList();
	}

	private async Task<GitHubRepositoryStatus> GetRepositoryStatusAsync(string owner,
		GitHubRepositoryDto repository, CancellationToken cancellationToken)
	{
		var issues = await _gitHubRestClient
			.TryGetAsync<List<GitHubIssueDto>>($"repos/{owner}/{repository.Name}/issues?state=open&per_page=100",
				_options.Token, cancellationToken)
			.ConfigureAwait(false) ?? [];

		var openIssues = issues
			.Where(issue => issue.PullRequest is null)
			.Select(ToSummary)
			.ToList();

		var openPullRequests = issues
			.Where(issue => issue.PullRequest is not null)
			.Select(ToSummary)
			.ToList();

		return new GitHubRepositoryStatus(repository.Name, repository.HtmlUrl, openIssues, openPullRequests);
	}

	private static GitHubIssueSummary ToSummary(GitHubIssueDto issue) =>
		new(issue.Number, issue.Title, issue.HtmlUrl);
}
