using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SimpleAuctions.Tests;

[Collection(AuctionApiCollection.Name)]
public sealed class UserAuthenticationTests(AuctionApiFixture fixture)
{
    [Fact]
    public async Task RegistrationLoginAndLogoutControlAccessToProfile()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        var email = TestData.UniqueEmail();

        using var unauthenticated = await client.GetAsync("/api/auth/login");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        using var registration = await client.PostAsJsonAsync(
            "/api/users", new { email, password = TestData.Password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using var registrationJson = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        Assert.Equal(email, registrationJson.RootElement.GetProperty("email").GetString());
        Assert.False(registrationJson.RootElement.TryGetProperty("passwordHash", out _));

        using var badLogin = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);

        using var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = TestData.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var profile = await client.GetAsync("/api/auth/login");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        using var profileJson = JsonDocument.Parse(await profile.Content.ReadAsStringAsync());
        Assert.Equal(email, profileJson.RootElement.GetProperty("user").GetProperty("email").GetString());

        using var logout = await client.DeleteAsync("/api/auth/login");
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var loggedOut = await client.GetAsync("/api/auth/login");
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOut.StatusCode);
    }
}
