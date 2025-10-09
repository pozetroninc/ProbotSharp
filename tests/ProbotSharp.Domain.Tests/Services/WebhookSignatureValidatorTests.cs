// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

using ProbotSharp.Domain.Services;

namespace ProbotSharp.Domain.Tests.Services;

public class WebhookSignatureValidatorTests
{
    private readonly WebhookSignatureValidator _validator = new();

    [Fact]
    public void IsSignatureValid_WhenSignatureMatches_ShouldReturnTrue()
    {
        const string payload = "{\"ok\":true}";
        const string secret = "secret";
        var signature = CreateSignature(payload, secret);

        var result = _validator.IsSignatureValid(payload, secret, signature);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSignatureValid_WhenSignatureDoesNotMatch_ShouldReturnFalse()
    {
        const string payload = "{\"ok\":true}";
        const string secret = "secret";
        var result = _validator.IsSignatureValid(payload, secret, "sha256=" + new string('0', 64));

        result.Should().BeFalse();
    }

    private static string CreateSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}

