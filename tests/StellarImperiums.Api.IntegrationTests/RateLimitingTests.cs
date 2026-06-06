using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace StellarImperiums.Api.IntegrationTests;

public sealed class RateLimitedApiFactory : StellarApiFactory
{
    protected override int LoginPermitLimit => 5;
}

public class RateLimitingTests(RateLimitedApiFactory factory) : IClassFixture<RateLimitedApiFactory>
{
    private sealed record LoginPayload(string Email, string Password);

    [Fact]
    public async Task Login_BeyondFiveAttemptsPerMinute_Returns429()
    {
        var client = factory.CreateApiClient();
        var payload = new LoginPayload("ratelimit@stellar.io", "Wrong-Password-99!");

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", payload);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"attempt {attempt}");
        }

        var sixth = await client.PostAsJsonAsync("/api/v1/auth/login", payload);
        sixth.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
