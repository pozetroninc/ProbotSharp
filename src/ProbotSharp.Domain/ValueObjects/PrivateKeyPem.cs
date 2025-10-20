// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

namespace ProbotSharp.Domain.ValueObjects;

/// <summary>
/// Represents a private key in PEM format for GitHub App authentication.
/// </summary>
public sealed record class PrivateKeyPem
{
    private PrivateKeyPem(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the PEM-formatted private key value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new private key PEM from a string.
    /// </summary>
    /// <param name="pem">The PEM-formatted private key.</param>
    /// <returns>A new private key PEM instance.</returns>
    public static PrivateKeyPem Create(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
        {
            throw new ArgumentException("Private key PEM cannot be null or whitespace.", nameof(pem));
        }

        var normalized = pem.Trim();
        if (!normalized.StartsWith("-----BEGIN", StringComparison.Ordinal) || !normalized.Contains("PRIVATE KEY") || !normalized.EndsWith("-----END PRIVATE KEY-----", StringComparison.Ordinal))
        {
            throw new ArgumentException("Private key PEM must contain valid BEGIN/END markers.", nameof(pem));
        }

        ValidatePem(normalized);

        return new PrivateKeyPem(normalized);
    }

    /// <summary>
    /// Creates an RSA instance from this private key.
    /// </summary>
    /// <returns>An RSA instance initialized with this private key.</returns>
    public RSA CreateRsa()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(this.Value);
        return rsa;
    }

    /// <summary>
    /// Returns the PEM-formatted private key as a string.
    /// </summary>
    /// <returns>The PEM-formatted private key.</returns>
    public override string ToString() => this.Value;

    private static void ValidatePem(string pem)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Private key PEM is not a valid RSA key.", nameof(pem), ex);
        }
    }
}
