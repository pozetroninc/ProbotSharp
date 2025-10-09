// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

namespace ProbotSharp.Domain.ValueObjects;

public sealed record class PrivateKeyPem
{
    private PrivateKeyPem(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

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

    public RSA CreateRsa()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(this.Value);
        return rsa;
    }

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

