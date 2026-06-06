using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace StellarImperiums.Api.IntegrationTests;

[Collection("api")]
public class AuthFlowTests(StellarApiFactory factory)
{
    private sealed record RegisterPayload(string Username, string Email, string Password);
    private sealed record LoginPayload(string Email, string Password);

    private static RegisterPayload NewAccount()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        return new RegisterPayload($"Cmdr_{suffix}", $"{suffix}@stellar.io", "P@ssword12345!");
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task FullJourney_Register_Login_Me_Refresh_Logout()
    {
        var client = factory.CreateApiClient();
        var account = NewAccount();

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", account);
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginPayload(account.Email, account.Password));
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        login.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        cookies!.ShouldContain(c => c.StartsWith("refresh_token=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase), customMessage: string.Join('\n', cookies!));
        var loginBody = await ReadJsonAsync(login);
        var accessToken = loginBody.GetProperty("accessToken").GetString();
        accessToken.ShouldNotBeNullOrEmpty();
        loginBody.GetProperty("tokenType").GetString().ShouldBe("Bearer");
        loginBody.GetProperty("user").GetProperty("username").GetString().ShouldBe(account.Username);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var me = await client.SendAsync(meRequest);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
        var meBody = await ReadJsonAsync(me);
        meBody.GetProperty("email").GetString().ShouldBe(account.Email);
        meBody.GetProperty("lastLoginAt").ValueKind.ShouldNotBe(JsonValueKind.Null);

        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshBody = await ReadJsonAsync(refresh);
        refreshBody.GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();

        var logout = await client.PostAsync("/api/v1/auth/logout", null);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var refreshAfterLogout = await client.PostAsync("/api/v1/auth/refresh", null);
        refreshAfterLogout.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateApiClient();
        var account = NewAccount();
        await client.PostAsJsonAsync("/api/v1/auth/register", account);

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginPayload(account.Email, "Wrong-Password-99!"));

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = factory.CreateApiClient();

        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginPayload("ghost@stellar.io", "P@ssword12345!"));

        login.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = factory.CreateApiClient();

        var me = await client.GetAsync("/api/v1/users/me");

        me.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithReplayedRotatedToken_RevokesWholeFamily()
    {
        var rawClient = factory.CreateApiClient(handleCookies: false);
        var account = NewAccount();
        await rawClient.PostAsJsonAsync("/api/v1/auth/register", account);

        var login = await rawClient.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginPayload(account.Email, account.Password));
        var firstCookie = ExtractRefreshCookie(login);

        var firstRefresh = await SendRefreshAsync(rawClient, firstCookie);
        firstRefresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondCookie = ExtractRefreshCookie(firstRefresh);

        var replay = await SendRefreshAsync(rawClient, firstCookie);
        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var familyDead = await SendRefreshAsync(rawClient, secondCookie);
        familyDead.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var values).ShouldBeTrue();
        var cookie = values!.First(c => c.StartsWith("refresh_token="));
        return cookie.Split(';')[0];
    }

    private static Task<HttpResponseMessage> SendRefreshAsync(HttpClient client, string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        return client.SendAsync(request);
    }
}
