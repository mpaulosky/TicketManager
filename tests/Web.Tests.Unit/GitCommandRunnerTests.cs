// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     GitCommandRunnerTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : TicketManager
// Project Name :  Web.Tests.Unit
// =============================================

using Web.Services;

namespace Web.Tests.Unit;

public sealed class GitCommandRunnerTests : IDisposable
{
	private readonly string _repositoryDirectory =
		Path.Combine(Path.GetTempPath(), $"git-command-runner-tests-{Guid.NewGuid():N}");

	public GitCommandRunnerTests()
	{
		Directory.CreateDirectory(_repositoryDirectory);
	}

	public void Dispose()
	{
		if (Directory.Exists(_repositoryDirectory))
		{
			Directory.Delete(_repositoryDirectory, recursive: true);
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public async Task RunAsync_NullOrWhitespaceWorkingDirectory_ThrowsArgumentException(string? workingDirectory)
	{
		// Arrange
		var runner = new GitCommandRunner();

		// Act
		var act = async () => await runner.RunAsync(workingDirectory!, "status");

		// Assert
		await act.Should().ThrowAsync<ArgumentException>();
	}

	[Fact]
	public async Task RunAsync_NullArguments_ThrowsArgumentNullException()
	{
		// Arrange
		var runner = new GitCommandRunner();

		// Act
		var act = async () => await runner.RunAsync(_repositoryDirectory, null!);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task RunAsync_SuccessfulCommandWithOutput_ReturnsTrimmedOutput()
	{
		// Arrange
		var runner = new GitCommandRunner();
		await runner.RunAsync(_repositoryDirectory, "init", "--quiet");

		// Act
		var result = await runner.RunAsync(_repositoryDirectory, "rev-parse", "--show-toplevel");

		// Assert
		result.Should().NotBeNullOrWhiteSpace();
		Path.GetFullPath(result!).TrimEnd(Path.DirectorySeparatorChar)
			.Should().Be(Path.GetFullPath(_repositoryDirectory).TrimEnd(Path.DirectorySeparatorChar));
	}

	[Fact]
	public async Task RunAsync_SuccessfulCommandWithEmptyOutput_ReturnsNull()
	{
		// Arrange
		var runner = new GitCommandRunner();
		await runner.RunAsync(_repositoryDirectory, "init", "--quiet");

		// Act
		var result = await runner.RunAsync(_repositoryDirectory, "status", "--porcelain");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task RunAsync_NonZeroExitCode_ThrowsInvalidOperationExceptionWithStandardError()
	{
		// Arrange
		var runner = new GitCommandRunner();
		await runner.RunAsync(_repositoryDirectory, "init", "--quiet");

		// Act
		var act = async () => await runner.RunAsync(_repositoryDirectory, "definitely-not-a-git-command");

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
