using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SimpleAuctions.Repositories;

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

        // Verify that unauthenticated users cannot access admin endpoints
        using var unauthenticatedAdmin = await client.GetAsync("/api/auth/admin");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedAdmin.StatusCode);

        using var registration = await client.PostAsJsonAsync(
            "/api/users", new { email, password = TestData.Password });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var registrationJson = JsonDocument.Parse(
            await registration.Content.ReadAsStringAsync());

        Assert.Equal(email, registrationJson.RootElement.GetProperty("email").GetString());
        Assert.False(registrationJson.RootElement.TryGetProperty("passwordHash", out _));

        using var badLogin = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);

        using var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = TestData.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Verify that regular users cannot access admin endpoints
        using var adminAccess = await client.GetAsync("/api/auth/admin");
        Assert.Equal(HttpStatusCode.Forbidden, adminAccess.StatusCode);

        using var profile = await client.GetAsync("/api/auth/login");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        using var profileJson = JsonDocument.Parse(
            await profile.Content.ReadAsStringAsync());

        Assert.Equal(
            email,
            profileJson.RootElement.GetProperty("user")
                .GetProperty("email").GetString());

        using var logout = await client.DeleteAsync("/api/auth/login");
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var loggedOut = await client.GetAsync("/api/auth/login");
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOut.StatusCode);
    }

    [Fact]
    public async Task AdminUser_ShouldHaveAdminAccess()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        var email = TestData.UniqueEmail();

        using var registration = await client.PostAsJsonAsync(
            "/api/users", new { email, password = TestData.Password });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var scope = fixture.Services.CreateScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<UserRepository>();

        var user = await repository.GetByEmail(email);

        Assert.NotNull(user);

        // Grant administrator privileges
        Assert.True(await repository.SetAdminAsync(user.Id, true));

        var updatedUser = await repository.GetById(user.Id);

        Assert.NotNull(updatedUser);
        Assert.True(updatedUser.IsAdmin);

        using var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = TestData.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Act
        using var adminAccess = await client.GetAsync("/api/auth/admin");

        // Assert
        Assert.Equal(HttpStatusCode.OK, adminAccess.StatusCode);
    }

    [Fact]
    public async Task UserWithRevokedAdminRole_ShouldBeForbidden()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        var email = TestData.UniqueEmail();

        using var registration = await client.PostAsJsonAsync(
            "/api/users", new { email, password = TestData.Password });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var scope = fixture.Services.CreateScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<UserRepository>();

        var user = await repository.GetByEmail(email);

        Assert.NotNull(user);

        Assert.True(await repository.SetAdminAsync(user.Id, true));

        using var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = TestData.Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Verify administrator access before revoking privileges
        using var initialAccess = await client.GetAsync("/api/auth/admin");

        Assert.Equal(HttpStatusCode.OK, initialAccess.StatusCode);

        // Revoke administrator privileges
        Assert.True(await repository.SetAdminAsync(user.Id, false));

        // Act
        using var adminAccess = await client.GetAsync("/api/auth/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, adminAccess.StatusCode);
    }

    [Fact]
    public async Task NewRegisteredUser_ShouldNotBeAdmin()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        var email = TestData.UniqueEmail();

        // Act
        using var registration = await client.PostAsJsonAsync(
            "/api/users", new { email, password = TestData.Password });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        using var scope = fixture.Services.CreateScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<UserRepository>();

        var user = await repository.GetByEmail(email);

        // Assert
        Assert.NotNull(user);
        Assert.False(user.IsAdmin);
    }
}