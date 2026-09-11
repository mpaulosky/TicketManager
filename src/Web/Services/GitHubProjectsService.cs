using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
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
/// user/organization, following the same resilient, best-effort conventions as
/// <see cref="GitHubMetadataProvider" />: typed HttpClient via IHttpClientFactory, JSON DTOs, and
/// graceful fallback on failure rather than throwing.
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification = "Injected into the GitHub Projects Razor page and consumed by Web tests.")]
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
	Justification =
		"This is a best-effort GitHub API lookup used to populate a dashboard; any failure should degrade gracefully rather than crash the page.")]
public sealed class GitHubProjectsService : IGitHubProjectsService
{
	public const string HttpClientName = "GitHubProjects";

	private const string AcceptHeader = "application/vnd.github+json";
	private const string UserAgent = "Articles-Web";
	private const string UserAgentVersion = "1.0";

	private readonly IHttpClientFactory _httpClientFactory;
	private readonly GitHubProjectsOptions _options;

	public GitHubProjectsService(IHttpClientFactory httpClientFactory, IOptions<GitHubProjectsOptions> options)
	{
		ArgumentNullException.ThrowIfNull(httpClientFactory);
		ArgumentNullException.ThrowIfNull(options);

		_httpClientFactory = httpClientFactory;
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

		var httpClient = _httpClientFactory.CreateClient(HttpClientName);

		var (repositories, error) = await GetRepositoriesAsync(httpClient, owner, cancellationToken)
			.ConfigureAwait(false);
		if (error is not null)
		{
			return GitHubProjectsResult.Empty(isAuthenticated, error);
		}

		var statuses = await Task.WhenAll(repositories.Select(repo =>
				GetRepositoryStatusAsync(httpClient, owner, repo, cancellationToken)))
			.ConfigureAwait(false);

		return new GitHubProjectsResult(isAuthenticated, statuses.OrderBy(status => status.Name,
			StringComparer.OrdinalIgnoreCase).ToList(), null);
	}

	private async Task<(IReadOnlyList<GitHubRepositoryDto> Repositories, string? Error)> GetRepositoriesAsync(
		HttpClient httpClient, string owner, CancellationToken cancellationToken)
	{
		var preferredSegment = string.Equals(_options.OwnerType, "Org", StringComparison.OrdinalIgnoreCase)
			? "orgs"
			: "users";
		var fallbackSegment = preferredSegment == "orgs" ? "users" : "orgs";

		var (repositories, notFound) =
			await TryGetRepositoriesAsync(httpClient, preferredSegment, owner, cancellationToken).ConfigureAwait(false);
		if (repositories is not null)
		{
			return (repositories, null);
		}

		if (notFound)
		{
			(repositories, _) = await TryGetRepositoriesAsync(httpClient, fallbackSegment, owner, cancellationToken)
				.ConfigureAwait(false);
			if (repositories is not null)
			{
				return (repositories, null);
			}
		}

		return ([], $"Could not find a GitHub user or organization named \"{owner}\".");
	}

	private async Task<(IReadOnlyList<GitHubRepositoryDto>? Repositories, bool NotFound)>
		TryGetRepositoriesAsync(HttpClient httpClient, string ownerSegment, string owner,
			CancellationToken cancellationToken)
	{
		try
		{
			using var request = CreateRequest(HttpMethod.Get,
				$"https://api.github.com/{ownerSegment}/{owner}/repos?per_page=100&sort=updated");

			using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				return (null, true);
			}

			if (!response.IsSuccessStatusCode)
			{
				return (null, false);
			}

			var repositories = await response.Content
				.ReadFromJsonAsync<List<GitHubRepositoryDto>>(cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return (repositories?.Where(repo => !repo.Fork && !repo.Archived).ToList()
				?? [], false);
		}
		catch (Exception)
		{
			return (null, false);
		}
	}

	private async Task<GitHubRepositoryStatus> GetRepositoryStatusAsync(HttpClient httpClient, string owner,
		GitHubRepositoryDto repository, CancellationToken cancellationToken)
	{
		var issues = await GetIssuesAsync(httpClient, owner, repository.Name, cancellationToken).ConfigureAwait(false);

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

	private async Task<IReadOnlyList<GitHubIssueDto>> GetIssuesAsync(HttpClient httpClient, string owner,
		string repo, CancellationToken cancellationToken)
	{
		try
		{
			using var request = CreateRequest(HttpMethod.Get,
				$"https://api.github.com/repos/{owner}/{repo}/issues?state=open&per_page=100");

			using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				return [];
			}

			var issues = await response.Content
				.ReadFromJsonAsync<List<GitHubIssueDto>>(cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return issues ?? [];
		}
		catch (Exception)
		{
			return [];
		}
	}

	private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
	{
		var request = new HttpRequestMessage(method, requestUri);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(AcceptHeader));
		request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, UserAgentVersion));

		if (_options.HasToken)
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
		}

		return request;
	}

	private static GitHubIssueSummary ToSummary(GitHubIssueDto issue) =>
		new(issue.Number, issue.Title, issue.HtmlUrl);
}
