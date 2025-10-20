// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Context;

namespace ProbotSharp.Application.Services;

/// <summary>
/// Service providing a fluent API for metadata operations on the current issue or pull request context.
/// </summary>
public sealed class MetadataService
{
    private readonly IMetadataPort _port;
    private readonly ProbotSharpContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataService"/> class.
    /// </summary>
    /// <param name="port">The metadata port instance.</param>
    /// <param name="context">The current Probot context.</param>
    public MetadataService(IMetadataPort port, ProbotSharpContext context)
    {
        this._port = port ?? throw new ArgumentNullException(nameof(port));
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Retrieves a metadata value for the current issue or pull request.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The metadata value if found, otherwise null.</returns>
    public Task<string?> GetAsync(string key, CancellationToken ct = default)
        => this._context.GetMetadataAsync(this._port, key, ct);

    /// <summary>
    /// Sets a metadata value for the current issue or pull request.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task SetAsync(string key, string value, CancellationToken ct = default)
        => this._context.SetMetadataAsync(this._port, key, value, ct);

    /// <summary>
    /// Checks if a metadata entry exists for the current issue or pull request.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the metadata exists, otherwise false.</returns>
    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => this._context.MetadataExistsAsync(this._port, key, ct);

    /// <summary>
    /// Deletes a metadata entry for the current issue or pull request.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task DeleteAsync(string key, CancellationToken ct = default)
        => this._context.DeleteMetadataAsync(this._port, key, ct);

    /// <summary>
    /// Retrieves all metadata entries for the current issue or pull request.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary containing all metadata key-value pairs.</returns>
    public Task<IDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
        => this._context.GetAllMetadataAsync(this._port, ct);
}
