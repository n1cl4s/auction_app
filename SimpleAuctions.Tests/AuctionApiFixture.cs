using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Testcontainers.MySql;

namespace SimpleAuctions.Tests;

[CollectionDefinition("Auction API", DisableParallelization = true)]
public sealed class AuctionApiCollection : ICollectionFixture<AuctionApiFixture>
{
    public const string Name = "Auction API";
}

public sealed class AuctionApiFixture : IAsyncLifetime
{
    private readonly MySqlContainer database = new MySqlBuilder("mysql:8.4")
        .WithDatabase("simple_auctions")
        .WithUsername("root")
        .WithPassword("test-password")
        .Build();

    private AuctionApiFactory factory = null!;

    // Provides access to services registered in the test application.
    public IServiceProvider Services => factory.Services;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        factory = new AuctionApiFactory(database.GetConnectionString());

        await using var connection = new MySqlConnection(database.GetConnectionString());
        await connection.OpenAsync();

        var schema = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Database", "script.sql"));

        var statements = schema.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(statement => string.Join(Environment.NewLine,
                statement.Split('\n').Where(line => !line.TrimStart()
                    .StartsWith("--", StringComparison.Ordinal))))
            .Where(statement => !string.IsNullOrWhiteSpace(statement));

        foreach (var statement in statements)
        {
            await using var command = new MySqlCommand(statement, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    public HttpClient CreateClient() => factory.CreateClient();

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new MySqlConnection(database.GetConnectionString());
        await connection.OpenAsync();

        foreach (var table in new[] { "bids", "auctions", "users" })
        {
            await using var command = new MySqlCommand(
                $"DELETE FROM {table};", connection);

            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        await database.DisposeAsync();
    }
}

internal sealed class AuctionApiFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuctionDatabase"] = connectionString
            }));
    }
}