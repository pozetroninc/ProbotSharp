// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Memory;

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Caching;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Caching;

public sealed class InMemoryAccessTokenCacheAdapterTests
{
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly InMemoryAccessTokenCacheAdapter _sut;

    public InMemoryAccessTokenCacheAdapterTests()
    {
        _sut = new InMemoryAccessTokenCacheAdapter(_memoryCache);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenNotCached()
    {
        var result = await _sut.GetAsync(InstallationId.Create(1));

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreToken_AndRetrieve()
    {
        var installationId = InstallationId.Create(1);
        var token = InstallationAccessToken.Create("token", DateTimeOffset.UtcNow.AddMinutes(5));

        await _sut.SetAsync(installationId, token);
        var result = await _sut.GetAsync(installationId);

        result.Should().NotBeNull();
        result.Should().Be(token);
    }
}

