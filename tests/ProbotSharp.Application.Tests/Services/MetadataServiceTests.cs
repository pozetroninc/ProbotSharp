// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Tests.Services;

public class MetadataServiceTests
{
    private readonly IMetadataPort _mockPort;
    private readonly ProbotSharpContext _context;

    public MetadataServiceTests()
    {
        _mockPort = Substitute.For<IMetadataPort>();
        _context = new ProbotSharpContext
        {
            Payload = JObject.Parse(@"{""issue"":{""number"":42}}"),
            Repository = new RepositoryInfo
            {
                Owner = "owner",
                Name = "repo"
            }
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateInstance()
    {
        // Act
        var service = new MetadataService(_mockPort, _context);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullPort_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MetadataService(null!, _context);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("port");
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MetadataService(_mockPort, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_ShouldCallPortWithCorrectParameters()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        var expectedValue = "test-value";
        _mockPort.GetAsync("owner", "repo", 42, "test-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(expectedValue));

        // Act
        var result = await service.GetAsync("test-key");

        // Assert
        result.Should().Be(expectedValue);
        await _mockPort.Received(1).GetAsync("owner", "repo", 42, "test-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenMetadataNotFound_ShouldReturnNull()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        _mockPort.GetAsync("owner", "repo", 42, "nonexistent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        // Act
        var result = await service.GetAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_ShouldPassTokenToPort()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _mockPort.GetAsync("owner", "repo", 42, "key", ct)
            .Returns(Task.FromResult<string?>("value"));

        // Act
        await service.GetAsync("key", ct);

        // Assert
        await _mockPort.Received(1).GetAsync("owner", "repo", 42, "key", ct);
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_ShouldCallPortWithCorrectParameters()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);

        // Act
        await service.SetAsync("key", "value");

        // Assert
        await _mockPort.Received(1).SetAsync("owner", "repo", 42, "key", "value", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_WithCancellationToken_ShouldPassTokenToPort()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await service.SetAsync("key", "value", ct);

        // Assert
        await _mockPort.Received(1).SetAsync("owner", "repo", 42, "key", "value", ct);
    }

    [Fact]
    public async Task SetAsync_WithEmptyValue_ShouldCallPort()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);

        // Act
        await service.SetAsync("key", string.Empty);

        // Assert
        await _mockPort.Received(1).SetAsync("owner", "repo", 42, "key", string.Empty, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenMetadataExists_ShouldReturnTrue()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        _mockPort.ExistsAsync("owner", "repo", 42, "existing-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await service.ExistsAsync("existing-key");

        // Assert
        result.Should().BeTrue();
        await _mockPort.Received(1).ExistsAsync("owner", "repo", 42, "existing-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_WhenMetadataDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        _mockPort.ExistsAsync("owner", "repo", 42, "nonexistent-key", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await service.ExistsAsync("nonexistent-key");

        // Assert
        result.Should().BeFalse();
        await _mockPort.Received(1).ExistsAsync("owner", "repo", 42, "nonexistent-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_WithCancellationToken_ShouldPassTokenToPort()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _mockPort.ExistsAsync("owner", "repo", 42, "key", ct)
            .Returns(Task.FromResult(true));

        // Act
        await service.ExistsAsync("key", ct);

        // Assert
        await _mockPort.Received(1).ExistsAsync("owner", "repo", 42, "key", ct);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ShouldCallPortWithCorrectParameters()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);

        // Act
        await service.DeleteAsync("key-to-delete");

        // Assert
        await _mockPort.Received(1).DeleteAsync("owner", "repo", 42, "key-to-delete", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WithCancellationToken_ShouldPassTokenToPort()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Act
        await service.DeleteAsync("key", ct);

        // Assert
        await _mockPort.Received(1).DeleteAsync("owner", "repo", 42, "key", ct);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllMetadata()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        var expectedMetadata = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        _mockPort.GetAllAsync("owner", "repo", 42, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, string>>(expectedMetadata));

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedMetadata);
        await _mockPort.Received(1).GetAllAsync("owner", "repo", 42, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WhenNoMetadata_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        var emptyMetadata = new Dictionary<string, string>();
        _mockPort.GetAllAsync("owner", "repo", 42, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDictionary<string, string>>(emptyMetadata));

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
        await _mockPort.Received(1).GetAllAsync("owner", "repo", 42, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WithCancellationToken_ShouldPassTokenToPort()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var metadata = new Dictionary<string, string>();
        _mockPort.GetAllAsync("owner", "repo", 42, ct)
            .Returns(Task.FromResult<IDictionary<string, string>>(metadata));

        // Act
        await service.GetAllAsync(ct);

        // Assert
        await _mockPort.Received(1).GetAllAsync("owner", "repo", 42, ct);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task RealWorldScenario_FullWorkflow_ShouldWork()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        _mockPort.ExistsAsync("owner", "repo", 42, "workflow-state", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false), Task.FromResult(true));
        _mockPort.GetAsync("owner", "repo", 42, "workflow-state", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("in-progress"));

        // Act & Assert
        // 1. Check if exists (should not exist initially)
        var exists = await service.ExistsAsync("workflow-state");
        exists.Should().BeFalse();

        // 2. Set the value
        await service.SetAsync("workflow-state", "in-progress");
        await _mockPort.Received(1).SetAsync("owner", "repo", 42, "workflow-state", "in-progress", Arg.Any<CancellationToken>());

        // 3. Check if exists (should exist now)
        exists = await service.ExistsAsync("workflow-state");
        exists.Should().BeTrue();

        // 4. Get the value
        var value = await service.GetAsync("workflow-state");
        value.Should().Be("in-progress");

        // 5. Delete the value
        await service.DeleteAsync("workflow-state");
        await _mockPort.Received(1).DeleteAsync("owner", "repo", 42, "workflow-state", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MultipleKeys_ShouldHandleIndependently()
    {
        // Arrange
        var service = new MetadataService(_mockPort, _context);
        _mockPort.GetAsync("owner", "repo", 42, "key1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("value1"));
        _mockPort.GetAsync("owner", "repo", 42, "key2", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("value2"));

        // Act
        var value1 = await service.GetAsync("key1");
        var value2 = await service.GetAsync("key2");

        // Assert
        value1.Should().Be("value1");
        value2.Should().Be("value2");
        await _mockPort.Received(1).GetAsync("owner", "repo", 42, "key1", Arg.Any<CancellationToken>());
        await _mockPort.Received(1).GetAsync("owner", "repo", 42, "key2", Arg.Any<CancellationToken>());
    }

    #endregion
}
