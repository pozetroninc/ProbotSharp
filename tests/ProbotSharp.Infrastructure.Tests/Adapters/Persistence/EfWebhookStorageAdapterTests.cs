// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Persistence;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Persistence;

public sealed class EfWebhookStorageAdapterTests : IDisposable
{
    private readonly ProbotSharpDbContext _dbContext;
    private readonly EfWebhookStorageAdapter _sut;
    private readonly ILogger<EfWebhookStorageAdapter> _logger = Substitute.For<ILogger<EfWebhookStorageAdapter>>();
    private bool _disposed;

    public EfWebhookStorageAdapterTests()
    {
        var options = new DbContextOptionsBuilder<ProbotSharpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ProbotSharpDbContext(options);
        _sut = new EfWebhookStorageAdapter(_dbContext, _logger);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _dbContext?.Dispose();
            _disposed = true;
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistDelivery_WhenNotExists()
    {
        var delivery = CreateDelivery();

        var result = await _sut.SaveAsync(delivery);

        result.IsSuccess.Should().BeTrue();
        (await _dbContext.WebhookDeliveries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_ShouldBeIdempotent_WhenDeliveryAlreadyExists()
    {
        var delivery = CreateDelivery();
        await _sut.SaveAsync(delivery);

        var result = await _sut.SaveAsync(delivery);

        result.IsSuccess.Should().BeTrue();
        (await _dbContext.WebhookDeliveries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDelivery_WhenExists()
    {
        var delivery = CreateDelivery();
        await _sut.SaveAsync(delivery);

        var result = await _sut.GetAsync(delivery.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(delivery.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _sut.GetAsync(DeliveryId.Create("missing"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    private static WebhookDelivery CreateDelivery()
        => WebhookDelivery.Create(
            DeliveryId.Create(Guid.NewGuid().ToString()),
            WebhookEventName.Create("push"),
            DateTimeOffset.UtcNow,
            WebhookPayload.Create("{\"ok\":true}"),
            InstallationId.Create(1));
}
