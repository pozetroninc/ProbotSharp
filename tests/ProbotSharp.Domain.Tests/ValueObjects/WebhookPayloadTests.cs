// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Tests.ValueObjects;

public class WebhookPayloadTests
{
    [Fact]
    public void Create_WithValidJson_ShouldReturnInstance()
    {
        const string json = "{\"name\":\"test\"}";

        var payload = WebhookPayload.Create(json);

        payload.RawBody.Should().Be(json);
        payload.RootElement.GetProperty("name").GetString().Should().Be("test");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidBody_ShouldThrow(string body)
    {
        var act = () => WebhookPayload.Create(body!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidJson_ShouldThrow()
    {
        var act = () => WebhookPayload.Create("{invalid json}");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetProperty_WithExistingProperty_ShouldReturnValue()
    {
        var payload = WebhookPayload.Create("{\"number\": 42}");

        payload.GetProperty<int>("number").Should().Be(42);
    }

    [Fact]
    public void GetProperty_WithMissingProperty_ShouldThrow()
    {
        var payload = WebhookPayload.Create("{\"number\": 42}");

        var act = () => payload.GetProperty<int>("missing");

        act.Should().Throw<KeyNotFoundException>();
    }
}
