using System.Net.Http.Json;

namespace SimpleAuctions.Tests;

internal static class TestData
{
    public const string Password = "TestPassword123!";

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.test";

    public static async Task RegisterAndLoginAsync(HttpClient client, string email)
    {
        using var registration = await client.PostAsJsonAsync("/api/users", new { email, password = Password });
        registration.EnsureSuccessStatusCode();

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
    }
}
