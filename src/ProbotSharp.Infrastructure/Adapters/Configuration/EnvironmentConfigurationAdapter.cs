// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.Configuration;

/// <summary>
/// Persists application credentials and webhook proxy details to a local .env file and process environment.
/// </summary>
public sealed class EnvironmentConfigurationAdapter : IEnvironmentConfigurationPort
{
    private static readonly string[] ManagedKeys =
    {
        "APP_ID",
        "PRIVATE_KEY_BASE64",
        "WEBHOOK_SECRET",
        "GITHUB_APP_CLIENT_ID",
        "GITHUB_APP_CLIENT_SECRET",
        "WEBHOOK_PROXY_URL",
    };

    private readonly string _envFilePath;
    private readonly ILogger<EnvironmentConfigurationAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentConfigurationAdapter"/> class.
    /// </summary>
    /// <param name="hostEnvironment">The host environment for determining file paths.</param>
    /// <param name="logger">The logger instance.</param>
    public EnvironmentConfigurationAdapter(IHostEnvironment hostEnvironment, ILogger<EnvironmentConfigurationAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        this._logger = logger;
        this._envFilePath = Path.Combine(hostEnvironment.ContentRootPath, ".env.local");
    }

    /// <inheritdoc />
    public async Task<Result> SaveAppCredentialsAsync(
        GitHubAppId appId,
        PrivateKeyPem privateKey,
        string webhookSecret,
        string? clientId = null,
        string? clientSecret = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(appId);
        ArgumentNullException.ThrowIfNull(privateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookSecret);

        try
        {
            var entries = await this.ReadEntriesAsync(cancellationToken).ConfigureAwait(false);

            entries["APP_ID"] = appId.Value.ToString(CultureInfo.InvariantCulture);
            entries["PRIVATE_KEY_BASE64"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey.Value));
            entries["WEBHOOK_SECRET"] = webhookSecret;

            if (!string.IsNullOrWhiteSpace(clientId))
            {
                entries["GITHUB_APP_CLIENT_ID"] = clientId;
            }

            if (!string.IsNullOrWhiteSpace(clientSecret))
            {
                entries["GITHUB_APP_CLIENT_SECRET"] = clientSecret;
            }

            await this.WriteEntriesAsync(entries, cancellationToken).ConfigureAwait(false);
            this.SetProcessEnvironment(entries);

            return Result.Success();
        }
        catch (IOException ex)
        {
            LogMessages.EnvironmentWriteFailed(this._logger, this._envFilePath, ex);
            return Result.Failure("environment_write_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogMessages.EnvironmentWriteFailed(this._logger, this._envFilePath, ex);
            return Result.Failure("environment_write_failed", ex.Message);
        }
        catch (Exception ex)
        {
            LogMessages.EnvironmentWriteFailed(this._logger, this._envFilePath, ex);
            return Result.Failure("environment_write_failed", ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveWebhookProxyUrlAsync(string webhookProxyUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookProxyUrl);

        try
        {
            var entries = await this.ReadEntriesAsync(cancellationToken).ConfigureAwait(false);
            entries["WEBHOOK_PROXY_URL"] = webhookProxyUrl;

            await this.WriteEntriesAsync(entries, cancellationToken).ConfigureAwait(false);
            Environment.SetEnvironmentVariable("WEBHOOK_PROXY_URL", webhookProxyUrl);

            return Result.Success();
        }
        catch (IOException ex)
        {
            LogMessages.EnvironmentWriteFailed(this._logger, this._envFilePath, ex);
            return Result.Failure("environment_write_failed", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogMessages.EnvironmentWriteFailed(this._logger, this._envFilePath, ex);
            return Result.Failure("environment_write_failed", ex.Message);
        }
        catch (Exception ex)
        {
            LogMessages.EnvironmentWriteFailed(this._logger, this._envFilePath, ex);
            return Result.Failure("environment_write_failed", ex.Message);
        }
    }

    private async Task<Dictionary<string, string>> ReadEntriesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(this._envFilePath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        await foreach (var rawLine in ReadLinesAsync(this._envFilePath, cancellationToken).ConfigureAwait(false))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            entries[key] = value;
        }

        return entries;
    }

    private async Task WriteEntriesAsync(Dictionary<string, string> entries, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var directory = Path.GetDirectoryName(this._envFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var managed = entries
            .Where(pair => ManagedKeys.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(pair => Array.IndexOf(ManagedKeys, pair.Key), Comparer<int>.Default)
            .ToArray();

        var unmanaged = entries
            .Where(pair => !ManagedKeys.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await using var stream = new FileStream(this._envFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await writer.WriteLineAsync("# Managed by ProbotSharp.Infrastructure - credentials for local development").ConfigureAwait(false);
        foreach (var (key, value) in managed)
        {
            await writer.WriteLineAsync(FormattableString.Invariant($"{key}={value}")).ConfigureAwait(false);
        }

        if (unmanaged.Length > 0)
        {
            await writer.WriteLineAsync().ConfigureAwait(false);
            foreach (var (key, value) in unmanaged)
            {
                await writer.WriteLineAsync(FormattableString.Invariant($"{key}={value}")).ConfigureAwait(false);
            }
        }

        await writer.FlushAsync().ConfigureAwait(false);
    }

    private void SetProcessEnvironment(Dictionary<string, string> entries)
    {
        Environment.SetEnvironmentVariable("APP_ID", entries["APP_ID"]);
        Environment.SetEnvironmentVariable("WEBHOOK_SECRET", entries["WEBHOOK_SECRET"]);
        Environment.SetEnvironmentVariable("PRIVATE_KEY_BASE64", entries["PRIVATE_KEY_BASE64"]);

        if (entries.TryGetValue("GITHUB_APP_CLIENT_ID", out var savedClientId))
        {
            Environment.SetEnvironmentVariable("GITHUB_APP_CLIENT_ID", savedClientId);
        }

        if (entries.TryGetValue("GITHUB_APP_CLIENT_SECRET", out var savedClientSecret))
        {
            Environment.SetEnvironmentVariable("GITHUB_APP_CLIENT_SECRET", savedClientSecret);
        }
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(
        string path,
        [global::System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            yield return line;
        }
    }
}
