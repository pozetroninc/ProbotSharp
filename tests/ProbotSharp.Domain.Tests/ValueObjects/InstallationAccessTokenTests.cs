// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class InstallationAccessTokenTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldTrimValue()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        var token = InstallationAccessToken.Create("  token  ", expiresAt);

        token.Value.Should().Be("token");
        token.ExpiresAt.Should().Be(expiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidValue_ShouldThrow(string value)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        var act = () => InstallationAccessToken.Create(value!, expiresAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithExpiredTimestamp_ShouldThrow()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var act = () => InstallationAccessToken.Create("token", expiresAt);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenNowAfterExpiry()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(1);
        var token = InstallationAccessToken.Create("token", expiresAt);

        token.IsExpired(expiresAt.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenNowBeforeExpiry()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var token = InstallationAccessToken.Create("token", expiresAt);

        token.IsExpired(expiresAt.AddMinutes(-1)).Should().BeFalse();
    }
}
