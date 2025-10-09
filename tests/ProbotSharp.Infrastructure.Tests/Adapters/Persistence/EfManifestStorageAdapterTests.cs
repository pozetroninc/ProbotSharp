// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ProbotSharp.Infrastructure.Adapters.Persistence;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Persistence;

public sealed class EfManifestStorageAdapterTests
{
    private readonly ProbotSharpDbContext _dbContext;
    private readonly EfManifestStorageAdapter _sut;
    private readonly ILogger<EfManifestStorageAdapter> _logger = Substitute.For<ILogger<EfManifestStorageAdapter>>();

    public EfManifestStorageAdapterTests()
    {
        var options = new DbContextOptionsBuilder<ProbotSharpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ProbotSharpDbContext(options);
        _sut = new EfManifestStorageAdapter(_dbContext, _logger);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistManifest_WhenNotExists()
    {
        var manifestJson = "{\"name\":\"test-app\",\"url\":\"https://example.com\"}";

        var result = await _sut.SaveAsync(manifestJson);

        result.IsSuccess.Should().BeTrue();
        (await _dbContext.GitHubAppManifests.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_ShouldUpdateManifest_WhenAlreadyExists()
    {
        var manifestJson1 = "{\"name\":\"test-app-1\"}";
        var manifestJson2 = "{\"name\":\"test-app-2\"}";

        await _sut.SaveAsync(manifestJson1);
        var result = await _sut.SaveAsync(manifestJson2);

        result.IsSuccess.Should().BeTrue();
        (await _dbContext.GitHubAppManifests.CountAsync()).Should().Be(1);
        var manifest = await _dbContext.GitHubAppManifests.FirstAsync();
        manifest.ManifestJson.Should().Be(manifestJson2);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnManifest_WhenExists()
    {
        var manifestJson = "{\"name\":\"test-app\"}";
        await _sut.SaveAsync(manifestJson);

        var result = await _sut.GetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().Be(manifestJson);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _sut.GetAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ShouldFail_WhenManifestJsonIsEmpty()
    {
        var act = async () => await _sut.SaveAsync(string.Empty);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAsync_ShouldFail_WhenManifestJsonIsNull()
    {
        var act = async () => await _sut.SaveAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
