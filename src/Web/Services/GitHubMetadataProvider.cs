// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitHubMetadataProvider.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web
// =============================================

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Web.Services;

[SuppressMessage("Design", "CA1515",
	Justification =
		"The metadata provider is intentionally consumed by the Blazor footer and its corresponding Web tests.")]
public sealed record GitHubMetadata(string ReleaseTag, string LastCommit);

[SuppressMessage("Design", "CA1515",
	Justification = "Injected into GitHubMetadataProvider and consumed by Web tests.")]
public interface IGitCommandRunner
{
	Task<string?> RunAsync(string workingDirectory, params string[] arguments);
}

/// <summary>
///   Runs the real <c>git</c> executable as a child process, used to discover the origin remote
///   and the local release tag/commit when GitHub's API is unavailable or the repository has no
///   releases yet.
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification = "Injected into GitHubMetadataProvider and consumed by Web tests.")]
public sealed class GitCommandRunner : IGitCommandRunner
{
	public async Task<string?> RunAsync(string workingDirectory, params string[] arguments)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
		ArgumentNullException.ThrowIfNull(arguments);

		var startInfo = new ProcessStartInfo("git")
		{
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start git.");
		var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
		var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
		await process.WaitForExitAsync().ConfigureAwait(false);

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException(error.Trim());
		}

		var trimmed = output.Trim();
		return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
	}
}

[SuppressMessage("Design", "CA1515",
	Justification =
		"The metadata provider is intentionally consumed by the Blazor footer and its corresponding Web tests.")]
public interface IGitHubMetadataProvider
{
	Task<GitHubMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///   Composes the local <c>git</c> checkout and the GitHub REST API to answer "what release and
///   commit is running right now": the origin remote identifies the owner/repo, the GitHub API is
///   tried first for the release tag and default-branch commit, and a local <c>git</c> lookup is
///   the fallback when the API has nothing (e.g. no releases published yet, or offline).
/// </summary>
[SuppressMessage("Design", "CA1515",
	Justification =
		"The metadata provider is intentionally consumed by the Blazor footer and its corresponding Web tests.")]
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
	Justification =
		"These are best-effort git/GitHub API lookups used to enrich footer metadata; any failure should fall back silently rather than propagate.")]
public sealed class GitHubMetadataProvider : IGitHubMetadataProvider
{
	private readonly IGitHubRestClient _gitHubRestClient;
	private readonly IGitCommandRunner _gitCommandRunner;

	public GitHubMetadataProvider(IGitHubRestClient gitHubRestClient, IGitCommandRunner gitCommandRunner)
	{
		ArgumentNullException.ThrowIfNull(gitHubRestClient);
		ArgumentNullException.ThrowIfNull(gitCommandRunner);

		_gitHubRestClient = gitHubRestClient;
		_gitCommandRunner = gitCommandRunner;
	}

	public async Task<GitHubMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default)
	{
		var remoteUrl = await GetOriginUrlAsync().ConfigureAwait(false);
		if (!TryParseGitHubRepository(remoteUrl, out var owner, out var repo))
		{
			return null;
		}

		var repoDetails = await GetRepositoryDetailsAsync(owner, repo, cancellationToken).ConfigureAwait(false);
		var releaseTag = await GetLatestReleaseTagAsync(owner, repo, cancellationToken).ConfigureAwait(false)
		                 ?? await GetLocalReleaseTagAsync().ConfigureAwait(false);
		var defaultBranch = repoDetails?.DefaultBranch ?? "main";
		var lastCommit = await GetLastCommitAsync(owner, repo, defaultBranch, cancellationToken).ConfigureAwait(false)
		                 ?? await GetLocalLastCommitAsync().ConfigureAwait(false);

		return new GitHubMetadata(
			releaseTag ?? "no release",
			lastCommit ?? "unknown");
	}

	[SuppressMessage("Design", "CA1054",
		Justification = "The Git origin string comes directly from git remotes and is normalized before being parsed.")]
	public static bool TryParseGitHubRepository(string? remoteUrl, out string owner, out string repo)
	{
		owner = string.Empty;
		repo = string.Empty;

		if (string.IsNullOrWhiteSpace(remoteUrl))
		{
			return false;
		}

		var normalized = remoteUrl.Trim();
		if (normalized.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
		{
			normalized = $"https://github.com/{normalized["git@github.com:".Length..]}";
		}
		else if (normalized.StartsWith("ssh://git@github.com/", StringComparison.OrdinalIgnoreCase))
		{
			normalized = normalized.Replace("ssh://git@github.com/", "https://github.com/", StringComparison.Ordinal);
		}

		if (!normalized.Contains("github.com", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		normalized = normalized.TrimEnd('/');
		var githubIndex = normalized.IndexOf("github.com", StringComparison.OrdinalIgnoreCase);
		var path = normalized[(githubIndex + "github.com".Length)..].TrimStart('/');
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (segments.Length < 2)
		{
			return false;
		}

		owner = segments[0];
		repo = segments[1];
		if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
		{
			repo = repo[..^4];
		}

		return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
	}

	private async Task<string?> GetOriginUrlAsync()
	{
		var configuredRepositoryUrl = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_URL")
		                              ?? Environment.GetEnvironmentVariable("REPOSITORY_URL");
		if (!string.IsNullOrWhiteSpace(configuredRepositoryUrl))
		{
			return configuredRepositoryUrl.Trim();
		}

		var configuredRepository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
		if (!string.IsNullOrWhiteSpace(configuredRepository))
		{
			return $"https://github.com/{configuredRepository.Trim()}.git";
		}

		var gitRoot = GetGitRootOrNull();
		if (gitRoot is null)
		{
			return null;
		}

		try
		{
			return await _gitCommandRunner.RunAsync(gitRoot, "remote", "get-url", "origin").ConfigureAwait(false);
		}
		catch
		{
			return null;
		}
	}

	private static HashSet<string> GetCandidateDirectories()
	{
		var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			Environment.CurrentDirectory, AppContext.BaseDirectory,
		};

		var current = new DirectoryInfo(Environment.CurrentDirectory);
		while (current is not null)
		{
			directories.Add(current.FullName);
			current = current.Parent;
		}

		var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
		while (baseDir is not null)
		{
			directories.Add(baseDir.FullName);
			baseDir = baseDir.Parent;
		}

		return directories;
	}

	private static string? FindGitRoot(string directory)
	{
		var current = new DirectoryInfo(directory);
		while (current is not null)
		{
			if (Directory.Exists(Path.Combine(current.FullName, ".git")))
			{
				return current.FullName;
			}

			current = current.Parent;
		}

		return null;
	}

	private static string? GetGitRootOrNull()
	{
		foreach (var candidate in GetCandidateDirectories())
		{
			var gitRoot = FindGitRoot(candidate);
			if (gitRoot is not null)
			{
				return gitRoot;
			}
		}

		return null;
	}

	private Task<RepositoryDetails?> GetRepositoryDetailsAsync(string owner, string repo,
		CancellationToken cancellationToken) =>
		_gitHubRestClient.TryGetAsync<RepositoryDetails>($"repos/{owner}/{repo}", cancellationToken: cancellationToken);

	private async Task<string?> GetLatestReleaseTagAsync(string owner, string repo,
		CancellationToken cancellationToken)
	{
		var release = await _gitHubRestClient
			.TryGetAsync<GitHubRelease>($"repos/{owner}/{repo}/releases/latest", cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return release?.TagName;
	}

	private async Task<string?> GetLastCommitAsync(string owner, string repo, string defaultBranch,
		CancellationToken cancellationToken)
	{
		var commit = await _gitHubRestClient
			.TryGetAsync<GitHubCommit>($"repos/{owner}/{repo}/commits/{Uri.EscapeDataString(defaultBranch)}",
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return commit?.Sha?[..7];
	}

	private async Task<string?> GetLocalReleaseTagAsync()
	{
		var gitRoot = GetGitRootOrNull();
		if (gitRoot is null)
		{
			return null;
		}

		try
		{
			var tag = await _gitCommandRunner.RunAsync(gitRoot, "describe", "--tags", "--abbrev=0")
				.ConfigureAwait(false);
			return string.IsNullOrWhiteSpace(tag) ? null : tag;
		}
		catch
		{
			return null;
		}
	}

	private async Task<string?> GetLocalLastCommitAsync()
	{
		var gitRoot = GetGitRootOrNull();
		if (gitRoot is null)
		{
			return null;
		}

		try
		{
			var sha = await _gitCommandRunner.RunAsync(gitRoot, "rev-parse", "--short", "HEAD").ConfigureAwait(false);
			return string.IsNullOrWhiteSpace(sha) ? "unknown" : sha;
		}
		catch
		{
			return "unknown";
		}
	}

	internal sealed class RepositoryDetails
	{
		[JsonPropertyName("default_branch")] public string DefaultBranch { get; set; } = string.Empty;
	}

	internal sealed class GitHubRelease
	{
		[JsonPropertyName("tag_name")] public string TagName { get; set; } = string.Empty;
	}

	internal sealed class GitHubCommit
	{
		public string? Sha { get; set; }
	}
}
