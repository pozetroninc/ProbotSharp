// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class WebhookSignatureTests
{
    [Fact]
    public void Create_WithValidSignature_ShouldReturnInstance()
    {
        var sig = WebhookSignature.Create("sha256=" + new string('a', 64));

        sig.Value.Should().Be("sha256=" + new string('a', 64));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("foo")]
    [InlineData("sha1=abc")]
    [InlineData("sha256=zzz")]
    public void Create_WithInvalidInput_ShouldThrow(string value)
    {
        var act = () => WebhookSignature.Create(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryValidatePayload_WithMatchingSignature_ShouldReturnTrue()
    {
        const string payload = "{\"ok\":true}";
        const string secret = "secret";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var signature = WebhookSignature.Create("sha256=" + Convert.ToHexString(hash).ToLowerInvariant());

        var result = WebhookSignature.TryValidatePayload(payload, secret, signature);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryValidatePayload_WithNonMatchingSignature_ShouldReturnFalse()
    {
        const string payload = "{\"ok\":true}";
        const string secret = "secret";
        var signature = WebhookSignature.Create("sha256=" + new string('0', 64));

        var result = WebhookSignature.TryValidatePayload(payload, secret, signature);

        result.Should().BeFalse();
    }
}
