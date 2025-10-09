// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using StackExchange.Redis;

using NSubstitute;

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Caching;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Caching;

public sealed class RedisAccessTokenCacheAdapterTests
{
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenRedisIsNull()
    {
        // Act & Assert
        var act = () => new RedisAccessTokenCacheAdapter(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenInstallationIdIsNull()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        var sut = new RedisAccessTokenCacheAdapter(redis);

        // Act & Assert
        var act = async () => await sut.GetAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetAsync_ShouldThrowArgumentNullException_WhenInstallationIdIsNull()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        var sut = new RedisAccessTokenCacheAdapter(redis);
        var token = InstallationAccessToken.Create("test-token", DateTimeOffset.UtcNow.AddMinutes(5));

        // Act & Assert
        var act = async () => await sut.SetAsync(null!, token);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetAsync_ShouldThrowArgumentNullException_WhenTokenIsNull()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        var sut = new RedisAccessTokenCacheAdapter(redis);
        var installationId = InstallationId.Create(1);

        // Act & Assert
        var act = async () => await sut.SetAsync(installationId, null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenRedisReturnsNull()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        var sut = new RedisAccessTokenCacheAdapter(redis);
        var installationId = InstallationId.Create(1);

        // Act
        var result = await sut.GetAsync(installationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnToken_WhenRedisReturnsValidJson()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var json = $@"{{""Token"":""test-token-value"",""ExpiresAt"":""{expiresAt:O}""}}";
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(new RedisValue(json));

        var sut = new RedisAccessTokenCacheAdapter(redis);
        var installationId = InstallationId.Create(1);

        // Act
        var result = await sut.GetAsync(installationId);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("test-token-value");
        result.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenRedisReturnsInvalidJson()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(new RedisValue("invalid json"));

        var sut = new RedisAccessTokenCacheAdapter(redis);
        var installationId = InstallationId.Create(1);

        // Act
        var result = await sut.GetAsync(installationId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreJsonWithExpiration()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        var sut = new RedisAccessTokenCacheAdapter(redis);
        var installationId = InstallationId.Create(1);
        var token = InstallationAccessToken.Create("test-token", DateTimeOffset.UtcNow.AddMinutes(5));

        // Act
        await sut.SetAsync(installationId, token);

        // Assert
        await database.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k.ToString().Contains("installation-token:1")),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>());
    }

    [Fact]
    public async Task SetAsync_ShouldUseCorrectKey()
    {
        // Arrange
        var redis = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        var sut = new RedisAccessTokenCacheAdapter(redis);
        var installationId = InstallationId.Create(123);
        var token = InstallationAccessToken.Create("test-token", DateTimeOffset.UtcNow.AddMinutes(5));

        // Act
        await sut.SetAsync(installationId, token);

        // Assert
        await database.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "installation-token:123"),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>());
    }
}
