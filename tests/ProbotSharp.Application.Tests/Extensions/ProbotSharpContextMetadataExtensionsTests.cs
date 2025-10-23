// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Octokit;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Contracts;

namespace ProbotSharp.Application.Tests.Extensions;

public class ProbotSharpContextMetadataExtensionsTests
{
    private readonly IMetadataPort _mockMetadataPort;

    public ProbotSharpContextMetadataExtensionsTests()
    {
        _mockMetadataPort = Substitute.For<IMetadataPort>();
    }

    #region GetMetadataAsync Tests

    [Fact]
    public async Task GetMetadataAsync_WithValidIssueContext_ShouldReturnMetadata()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);
        var expectedValue = "test-value";

        _mockMetadataPort.GetAsync("owner", "repo", 42, "key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(expectedValue));

        // Act
        var result = await context.GetMetadataAsync(_mockMetadataPort, "key");

        // Assert
        result.Should().Be(expectedValue);
        await _mockMetadataPort.Received(1).GetAsync("owner", "repo", 42, "key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_WithValidPullRequestContext_ShouldReturnMetadata()
    {
        // Arrange
        var payload = JObject.Parse(@"{""pull_request"":{""number"":123}}");
        var context = CreateContextWithRepository(payload);
        var expectedValue = "pr-value";

        _mockMetadataPort.GetAsync("owner", "repo", 123, "pr-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(expectedValue));

        // Act
        var result = await context.GetMetadataAsync(_mockMetadataPort, "pr-key");

        // Assert
        result.Should().Be(expectedValue);
        await _mockMetadataPort.Received(1).GetAsync("owner", "repo", 123, "pr-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetadataAsync_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithoutRepository(payload);

        // Act
        var act = async () => await context.GetMetadataAsync(_mockMetadataPort, "key");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires repository context");
    }

    [Fact]
    public async Task GetMetadataAsync_WithoutIssueOrPullRequest_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""other"":{""data"":""value""}}");
        var context = CreateContextWithRepository(payload);

        // Act
        var act = async () => await context.GetMetadataAsync(_mockMetadataPort, "key");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires issue or pull request context");
    }

    [Fact]
    public async Task GetMetadataAsync_WithEmptyPayload_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{}");
        var context = CreateContextWithRepository(payload);

        // Act
        var act = async () => await context.GetMetadataAsync(_mockMetadataPort, "key");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires issue or pull request context");
    }

    [Fact]
    public async Task GetMetadataAsync_WhenMetadataNotFound_ShouldReturnNull()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);

        _mockMetadataPort.GetAsync("owner", "repo", 42, "nonexistent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        // Act
        var result = await context.GetMetadataAsync(_mockMetadataPort, "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMetadataAsync_PrefersIssueNumberOverPullRequest()
    {
        // Arrange - Both issue and pull_request present
        var payload = JObject.Parse(@"{""issue"":{""number"":42},""pull_request"":{""number"":123}}");
        var context = CreateContextWithRepository(payload);

        _mockMetadataPort.GetAsync("owner", "repo", 42, "key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("value"));

        // Act
        await context.GetMetadataAsync(_mockMetadataPort, "key");

        // Assert - Should use issue number (42), not pull_request number (123)
        await _mockMetadataPort.Received(1).GetAsync("owner", "repo", 42, "key", Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetMetadataAsync Tests

    [Fact]
    public async Task SetMetadataAsync_WithValidIssueContext_ShouldSetMetadata()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);

        // Act
        await context.SetMetadataAsync(_mockMetadataPort, "key", "value");

        // Assert
        await _mockMetadataPort.Received(1).SetAsync("owner", "repo", 42, "key", "value", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetMetadataAsync_WithValidPullRequestContext_ShouldSetMetadata()
    {
        // Arrange
        var payload = JObject.Parse(@"{""pull_request"":{""number"":123}}");
        var context = CreateContextWithRepository(payload);

        // Act
        await context.SetMetadataAsync(_mockMetadataPort, "pr-key", "pr-value");

        // Assert
        await _mockMetadataPort.Received(1).SetAsync("owner", "repo", 123, "pr-key", "pr-value", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetMetadataAsync_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithoutRepository(payload);

        // Act
        var act = async () => await context.SetMetadataAsync(_mockMetadataPort, "key", "value");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires repository context");
    }

    [Fact]
    public async Task SetMetadataAsync_WithoutIssueOrPullRequest_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""other"":{""data"":""value""}}");
        var context = CreateContextWithRepository(payload);

        // Act
        var act = async () => await context.SetMetadataAsync(_mockMetadataPort, "key", "value");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires issue or pull request context");
    }

    #endregion

    #region MetadataExistsAsync Tests

    [Fact]
    public async Task MetadataExistsAsync_WhenMetadataExists_ShouldReturnTrue()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);

        _mockMetadataPort.ExistsAsync("owner", "repo", 42, "existing-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await context.MetadataExistsAsync(_mockMetadataPort, "existing-key");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MetadataExistsAsync_WhenMetadataDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);

        _mockMetadataPort.ExistsAsync("owner", "repo", 42, "nonexistent-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await context.MetadataExistsAsync(_mockMetadataPort, "nonexistent-key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MetadataExistsAsync_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithoutRepository(payload);

        // Act
        var act = async () => await context.MetadataExistsAsync(_mockMetadataPort, "key");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires repository context");
    }

    [Fact]
    public async Task MetadataExistsAsync_WithoutIssueOrPullRequest_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""other"":{""data"":""value""}}");
        var context = CreateContextWithRepository(payload);

        // Act
        var act = async () => await context.MetadataExistsAsync(_mockMetadataPort, "key");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires issue or pull request context");
    }

    #endregion

    #region DeleteMetadataAsync Tests

    [Fact]
    public async Task DeleteMetadataAsync_WithValidIssueContext_ShouldDeleteMetadata()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);

        // Act
        await context.DeleteMetadataAsync(_mockMetadataPort, "key-to-delete");

        // Assert
        await _mockMetadataPort.Received(1).DeleteAsync("owner", "repo", 42, "key-to-delete", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMetadataAsync_WithValidPullRequestContext_ShouldDeleteMetadata()
    {
        // Arrange
        var payload = JObject.Parse(@"{""pull_request"":{""number"":123}}");
        var context = CreateContextWithRepository(payload);

        // Act
        await context.DeleteMetadataAsync(_mockMetadataPort, "pr-key");

        // Assert
        await _mockMetadataPort.Received(1).DeleteAsync("owner", "repo", 123, "pr-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMetadataAsync_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithoutRepository(payload);

        // Act
        var act = async () => await context.DeleteMetadataAsync(_mockMetadataPort, "key");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires repository context");
    }

    [Fact]
    public async Task DeleteMetadataAsync_WithoutIssueOrPullRequest_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""other"":{""data"":""value""}}");
        var context = CreateContextWithRepository(payload);

        // Act
        var act = async () => await context.DeleteMetadataAsync(_mockMetadataPort, "key");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires issue or pull request context");
    }

    #endregion

    #region GetAllMetadataAsync Tests

    [Fact]
    public async Task GetAllMetadataAsync_WithValidIssueContext_ShouldReturnAllMetadata()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);
        var expectedMetadata = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        _mockMetadataPort.GetAllAsync("owner", "repo", 42, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, string>>(expectedMetadata));

        // Act
        var result = await context.GetAllMetadataAsync(_mockMetadataPort);

        // Assert
        result.Should().BeEquivalentTo(expectedMetadata);
        result.Should().ContainKeys("key1", "key2");
    }

    [Fact]
    public async Task GetAllMetadataAsync_WithValidPullRequestContext_ShouldReturnAllMetadata()
    {
        // Arrange
        var payload = JObject.Parse(@"{""pull_request"":{""number"":123}}");
        var context = CreateContextWithRepository(payload);
        var expectedMetadata = new Dictionary<string, string>
        {
            { "pr-key", "pr-value" }
        };

        _mockMetadataPort.GetAllAsync("owner", "repo", 123, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, string>>(expectedMetadata));

        // Act
        var result = await context.GetAllMetadataAsync(_mockMetadataPort);

        // Assert
        result.Should().BeEquivalentTo(expectedMetadata);
    }

    [Fact]
    public async Task GetAllMetadataAsync_WhenNoMetadataExists_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);
        var emptyMetadata = new Dictionary<string, string>();

        _mockMetadataPort.GetAllAsync("owner", "repo", 42, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, string>>(emptyMetadata));

        // Act
        var result = await context.GetAllMetadataAsync(_mockMetadataPort);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllMetadataAsync_WithNullRepository_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithoutRepository(payload);

        // Act
        var act = async () => await context.GetAllMetadataAsync(_mockMetadataPort);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires repository context");
    }

    [Fact]
    public async Task GetAllMetadataAsync_WithoutIssueOrPullRequest_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payload = JObject.Parse(@"{""other"":{""data"":""value""}}");
        var context = CreateContextWithRepository(payload);

        // Act
        var act = async () => await context.GetAllMetadataAsync(_mockMetadataPort);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Metadata requires issue or pull request context");
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task GetMetadataAsync_WithCancellationToken_ShouldPassTokenToPort()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        _mockMetadataPort.GetAsync("owner", "repo", 42, "key", ct)
            .Returns(Task.FromResult<string?>("value"));

        // Act
        await context.GetMetadataAsync(_mockMetadataPort, "key", ct);

        // Assert
        await _mockMetadataPort.Received(1).GetAsync("owner", "repo", 42, "key", ct);
    }

    [Fact]
    public async Task SetMetadataAsync_WithCancellationToken_ShouldPassTokenToPort()
    {
        // Arrange
        var payload = JObject.Parse(@"{""issue"":{""number"":42}}");
        var context = CreateContextWithRepository(payload);
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await context.SetMetadataAsync(_mockMetadataPort, "key", "value", ct);

        // Assert
        await _mockMetadataPort.Received(1).SetAsync("owner", "repo", 42, "key", "value", ct);
    }

    #endregion

    #region Helper Methods

    private static ProbotSharpContext CreateContextWithRepository(JObject payload)
    {
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();
        var repository = new RepositoryInfo(12345, "repo", "owner", "owner/repo");

        return new ProbotSharpContext(
            "test-delivery-id",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            repository,
            null);
    }

    private static ProbotSharpContext CreateContextWithoutRepository(JObject payload)
    {
        var logger = Substitute.For<ILogger>();
        var gitHub = Substitute.For<IGitHubClient>();
        var graphQL = Substitute.For<IGitHubGraphQlClient>();

        return new ProbotSharpContext(
            "test-delivery-id",
            "issues",
            "opened",
            payload,
            logger,
            gitHub,
            graphQL,
            null,
            null);
    }

    #endregion
}
