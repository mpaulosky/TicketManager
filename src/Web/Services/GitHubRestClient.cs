using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Web.Services;

[SuppressMessage("Design", "CA1515",
	Justification = "Injected into GitHubProjectsService and GitHubMetadataProvider, and consumed by Web tests.")]
public interface IGitHubRestClient
{
	Task<T?> TryGetAsync<T>(string path, string? token = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// The one place TicketManager talks HTTP to the GitHub REST API: builds the request (base URL,
/// Accept header, User-Agent, optional bearer token), and degrades any failure - a non-success
/// status, a transport exception, or a malformed body - to <see langword="null" /> rather than
/// throwing, logging a warning so a quiet dashboard is still diagnosable.
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification = "Injected into GitHubProjectsService and GitHubMetadataProvider, and consumed by Web tests.")]
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
	Justification =
		"Every caller of this client treats a failed GitHub lookup as best-effort; degrade to null instead of throwing.")]
public sealed class GitHubRestClient : IGitHubRestClient
{
	public const string HttpClientName = "GitHubProjects";
	public const string ApiBaseUrl = "https://api.github.com";

	private const string AcceptHeader = "application/vnd.github+json";
	private const string UserAgent = "TicketManager-Web";
	private const string UserAgentVersion = "1.0";

	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<GitHubRestClient> _logger;

	public GitHubRestClient(IHttpClientFactory httpClientFactory, ILogger<GitHubRestClient> logger)
	{
		ArgumentNullException.ThrowIfNull(httpClientFactory);
		ArgumentNullException.ThrowIfNull(logger);

		_httpClientFactory = httpClientFactory;
		_logger = logger;
	}

	public async Task<T?> TryGetAsync<T>(string path, string? token = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		try
		{
			var httpClient = _httpClientFactory.CreateClient(HttpClientName);

			using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/{path}");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(AcceptHeader));
			request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, UserAgentVersion));

			if (!string.IsNullOrWhiteSpace(token))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			}

			using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"GitHub API request to {Path} failed with status {StatusCode}.", path, (int)response.StatusCode);

				return default;
			}

			return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}
		catch (Exception exception)
		{
			_logger.LogWarning(exception, "GitHub API request to {Path} threw an exception.", path);

			return default;
		}
	}
}
