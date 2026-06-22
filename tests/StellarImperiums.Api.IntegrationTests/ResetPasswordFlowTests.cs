using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace StellarImperiums.Api.IntegrationTests;

[Collection("api")]
public class ResetPasswordFlowTests(StellarApiFactory factory)
{
    private sealed record RegisterPayload(string Username, string Email, string Password);
    private sealed record ForgotPasswordPayload(string Email);
    private sealed record ResetPasswordPayload(string Token, string NewPassword);

    private static RegisterPayload NewAccount()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        return new RegisterPayload($"Cmdr_{suffix}", $"{suffix}@stellar.io", "P@ssword12345!");
    }

    [Fact]
    public async Task ForgotPassword_ForExistingUser_Returns202()
    {
        var client = factory.CreateApiClient();
        var account = NewAccount();
        await client.PostAsJsonAsync("/api/v1/auth/register", account);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/forgot-password",
            new ForgotPasswordPayload(account.Email));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_Returns202()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/forgot-password",
            new ForgotPasswordPayload("ghost-nobody@stellar.io"));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new ResetPasswordPayload("bogus-token-that-does-not-exist", "NewP@ssword12345!"));

        ((int)response.StatusCode).ShouldBe(400);
    }
}
