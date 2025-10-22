// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Metadata;

namespace ProbotSharp.Domain.Tests.Metadata;

public class IssueMetadataTests
{
    [Fact]
    public void Constructor_WithRequiredProperties_ShouldCreateInstance()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "octocat",
            RepositoryName = "Hello-World",
            IssueNumber = 42,
            Key = "status",
            Value = "in-progress",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Should().NotBeNull();
        metadata.RepositoryOwner.Should().Be("octocat");
        metadata.RepositoryName.Should().Be("Hello-World");
        metadata.IssueNumber.Should().Be(42);
        metadata.Key.Should().Be("status");
        metadata.Value.Should().Be("in-progress");
        metadata.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        metadata.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Id_ShouldBeSettableAndGettable()
    {
        // Arrange
        var metadata = new IssueMetadata
        {
            Id = 123,
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Id.Should().Be(123);
    }

    [Fact]
    public void RepositoryOwner_ShouldBeSettable()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "github",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.RepositoryOwner.Should().Be("github");
    }

    [Fact]
    public void RepositoryName_ShouldBeSettable()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "my-awesome-repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.RepositoryName.Should().Be("my-awesome-repo");
    }

    [Fact]
    public void IssueNumber_ShouldBeSettable()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 999,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.IssueNumber.Should().Be(999);
    }

    [Fact]
    public void Key_ShouldBeSettable()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "custom-key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Key.Should().Be("custom-key");
    }

    [Fact]
    public void Value_ShouldBeSettable()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "custom-value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Value.Should().Be("custom-value");
    }

    [Fact]
    public void CreatedAt_ShouldBeSettable()
    {
        // Arrange
        var createdAt = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = createdAt,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void UpdatedAt_ShouldBeSettable()
    {
        // Arrange
        var updatedAt = new DateTime(2023, 2, 20, 14, 45, 0, DateTimeKind.Utc);

        // Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = updatedAt
        };

        // Assert
        metadata.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void UpdatedAt_ShouldBeAfterOrEqualToCreatedAt_InTypicalUsage()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var updatedAt = createdAt.AddHours(1);

        // Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        metadata.UpdatedAt.Should().BeAfter(metadata.CreatedAt);
    }

    [Theory]
    [InlineData("octocat", "Hello-World", 1, "status", "open")]
    [InlineData("microsoft", "vscode", 12345, "assignee", "user1")]
    [InlineData("github", "docs", 999, "label", "bug")]
    public void Properties_ShouldSupportVariousCombinations(
        string owner,
        string repo,
        int issueNumber,
        string key,
        string value)
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = owner,
            RepositoryName = repo,
            IssueNumber = issueNumber,
            Key = key,
            Value = value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.RepositoryOwner.Should().Be(owner);
        metadata.RepositoryName.Should().Be(repo);
        metadata.IssueNumber.Should().Be(issueNumber);
        metadata.Key.Should().Be(key);
        metadata.Value.Should().Be(value);
    }

    [Fact]
    public void Value_ShouldSupportJsonContent()
    {
        // Arrange
        var jsonValue = "{\"state\":\"processing\",\"progress\":75}";

        // Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "workflow-state",
            Value = jsonValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Value.Should().Be(jsonValue);
        metadata.Value.Should().Contain("state");
        metadata.Value.Should().Contain("processing");
    }

    [Fact]
    public void Value_ShouldSupportLongText()
    {
        // Arrange
        var longValue = new string('a', 5000);

        // Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "description",
            Value = longValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Value.Should().HaveLength(5000);
    }

    [Fact]
    public void Key_ShouldSupportDottedNotation()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "workflow.state.current",
            Value = "running",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Key.Should().Be("workflow.state.current");
        metadata.Key.Should().Contain(".");
    }

    [Fact]
    public void RepositoryOwner_ShouldSupportOrganizationNames()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "my-org-123",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.RepositoryOwner.Should().Be("my-org-123");
        metadata.RepositoryOwner.Should().Contain("-");
    }

    [Fact]
    public void RepositoryName_ShouldSupportHyphensAndUnderscores()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "my_awesome-repo_123",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.RepositoryName.Should().Be("my_awesome-repo_123");
    }

    [Fact]
    public void IssueNumber_ShouldSupportLargeNumbers()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = int.MaxValue,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.IssueNumber.Should().Be(int.MaxValue);
    }

    [Fact]
    public void RealWorldScenario_TrackingPullRequestStatus_ShouldWork()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        // Act
        var metadata = new IssueMetadata
        {
            Id = 1,
            RepositoryOwner = "acme-corp",
            RepositoryName = "api-service",
            IssueNumber = 456,
            Key = "pr.review.status",
            Value = "changes-requested",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Assert
        metadata.Id.Should().Be(1);
        metadata.RepositoryOwner.Should().Be("acme-corp");
        metadata.RepositoryName.Should().Be("api-service");
        metadata.IssueNumber.Should().Be(456);
        metadata.Key.Should().Be("pr.review.status");
        metadata.Value.Should().Be("changes-requested");
        metadata.CreatedAt.Should().Be(createdAt);
        metadata.UpdatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void RealWorldScenario_StoringWorkflowState_ShouldWork()
    {
        // Arrange
        var workflowState = "{\"step\":\"build\",\"status\":\"success\",\"nextStep\":\"deploy\"}";

        // Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "dev-team",
            RepositoryName = "deployment-pipeline",
            IssueNumber = 789,
            Key = "workflow.state",
            Value = workflowState,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Key.Should().Be("workflow.state");
        metadata.Value.Should().Contain("build");
        metadata.Value.Should().Contain("deploy");
        metadata.UpdatedAt.Should().BeAfter(metadata.CreatedAt);
    }

    [Fact]
    public void RealWorldScenario_TrackingIssueAssignment_ShouldWork()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "open-source-project",
            RepositoryName = "main-repo",
            IssueNumber = 12,
            Key = "auto-assignment.attempted",
            Value = "true",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.RepositoryOwner.Should().Be("open-source-project");
        metadata.IssueNumber.Should().Be(12);
        metadata.Key.Should().Be("auto-assignment.attempted");
        metadata.Value.Should().Be("true");
    }

    [Fact]
    public void Id_ShouldDefaultToZeroIfNotSet()
    {
        // Arrange & Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        metadata.Id.Should().Be(0);
    }

    [Fact]
    public void CreatedAt_AndUpdatedAt_CanBeTheSame()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var metadata = new IssueMetadata
        {
            RepositoryOwner = "owner",
            RepositoryName = "repo",
            IssueNumber = 1,
            Key = "key",
            Value = "value",
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        // Assert
        metadata.CreatedAt.Should().Be(metadata.UpdatedAt);
    }
}
