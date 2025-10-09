// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class PrivateKeyPemTests
{
    [Fact]
    public void Create_WithValidPem_ShouldReturnInstance()
    {
        var pemValue = GeneratePkcs8Pem();

        var pem = PrivateKeyPem.Create(pemValue);

        pem.Value.Should().Be(pemValue.Trim());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid pem")]
    public void Create_WithInvalidPem_ShouldThrow(string value)
    {
        var act = () => PrivateKeyPem.Create(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithPemMissingBeginMarker_ShouldThrow()
    {
        var invalidPem = "INVALID-----\ndata\n-----END PRIVATE KEY-----";

        var act = () => PrivateKeyPem.Create(invalidPem);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*BEGIN/END markers*");
    }

    [Fact]
    public void Create_WithPemMissingPrivateKeyMarker_ShouldThrow()
    {
        var invalidPem = "-----BEGIN PUBLIC KEY-----\ndata\n-----END PUBLIC KEY-----";

        var act = () => PrivateKeyPem.Create(invalidPem);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*BEGIN/END markers*");
    }

    [Fact]
    public void Create_WithPemMissingEndMarker_ShouldThrow()
    {
        var invalidPem = "-----BEGIN PRIVATE KEY-----\ndata";

        var act = () => PrivateKeyPem.Create(invalidPem);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*BEGIN/END markers*");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimAndReturnInstance()
    {
        var pemValue = GeneratePkcs8Pem();
        var paddedPem = $"  {pemValue}  ";

        var pem = PrivateKeyPem.Create(paddedPem);

        pem.Value.Should().Be(pemValue.Trim());
    }

    [Fact]
    public void CreateRsa_ShouldReturnValidRsaInstance()
    {
        var pemValue = GeneratePkcs8Pem();
        var pem = PrivateKeyPem.Create(pemValue);

        using var rsa = pem.CreateRsa();

        rsa.Should().NotBeNull();
        rsa.KeySize.Should().Be(2048);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var pemValue = GeneratePkcs8Pem();
        var pem = PrivateKeyPem.Create(pemValue);

        var result = pem.ToString();

        result.Should().Be(pemValue.Trim());
    }

    [Fact]
    public void RecordEquality_WithSameValue_ShouldBeEqual()
    {
        var pemValue = GeneratePkcs8Pem();
        var pem1 = PrivateKeyPem.Create(pemValue);
        var pem2 = PrivateKeyPem.Create(pemValue);

        pem1.Should().Be(pem2);
        (pem1 == pem2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_WithDifferentValue_ShouldNotBeEqual()
    {
        var pemValue1 = GeneratePkcs8Pem();
        var pemValue2 = GeneratePkcs8Pem();
        var pem1 = PrivateKeyPem.Create(pemValue1);
        var pem2 = PrivateKeyPem.Create(pemValue2);

        pem1.Should().NotBe(pem2);
        (pem1 != pem2).Should().BeTrue();
    }

    private static string GeneratePkcs8Pem()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8Bytes = rsa.ExportPkcs8PrivateKey();
        var base64 = Convert.ToBase64String(pkcs8Bytes, Base64FormattingOptions.InsertLineBreaks);
        return $@"-----BEGIN PRIVATE KEY-----
{base64}
-----END PRIVATE KEY-----";
    }
}
