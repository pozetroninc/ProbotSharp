// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Infrastructure.Adapters.Persistence.Models;
using ProbotSharp.Domain.Abstractions;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Infrastructure.Adapters.Persistence;

/// <summary>
/// Entity Framework-based adapter for persisting webhook delivery records to a database.
/// </summary>
public sealed class EfWebhookStorageAdapter : IWebhookStoragePort
{
    private readonly ProbotSharpDbContext _dbContext;
    private readonly ILogger<EfWebhookStorageAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfWebhookStorageAdapter"/> class.
    /// </summary>
    /// <param name="dbContext">The database context for webhook storage operations.</param>
    /// <param name="logger">The logger instance.</param>
    public EfWebhookStorageAdapter(ProbotSharpDbContext dbContext, ILogger<EfWebhookStorageAdapter> logger)
    {
        this._dbContext = dbContext;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        try
        {
            var entity = WebhookDeliveryEntity.FromDomain(delivery);
            var exists = await this._dbContext.WebhookDeliveries
                .AsNoTracking()
                .AnyAsync(x => x.DeliveryId == entity.DeliveryId, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                return Result.Success();
            }

            await this._dbContext.WebhookDeliveries.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here to convert infrastructure errors to Result type
            this._logger.LogError(ex, "Failed to save webhook delivery {DeliveryId}", delivery.Id.Value);
            return Result.Failure("webhook_storage_save_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result<WebhookDelivery?>> GetAsync(DeliveryId deliveryId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deliveryId);

        try
        {
            var entity = await this._dbContext.WebhookDeliveries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeliveryId == deliveryId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Result<WebhookDelivery?>.Success(null);
            }

            var domainResult = entity.ToDomain();
            if (!domainResult.IsSuccess)
            {
                return Result<WebhookDelivery?>.Failure(
                    domainResult.Error ?? new Error("webhook_entity_mapping_failed", "Failed to map webhook entity to domain"));
            }

            return Result<WebhookDelivery?>.Success(domainResult.Value);
        }
        catch (Exception ex)
        {
            // CA1031: Catching general exception is intentional here to convert infrastructure errors to Result type
            this._logger.LogError(ex, "Failed to retrieve webhook delivery {DeliveryId}", deliveryId.Value);
            return Result<WebhookDelivery?>.Failure("webhook_storage_get_failed", ex.Message);
        }
    }
}

#pragma warning restore CA1848
