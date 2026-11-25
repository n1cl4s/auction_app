using MySqlConnector;

namespace SimpleAuctions.Tests;

[Collection(AuctionApiCollection.Name)]
public sealed class TestDatabaseTests(AuctionApiFixture fixture)
{
    [Fact]
    public async Task ResetRemovesCarAuctionDataAndItsDependencies()
    {
        await fixture.ResetDatabaseAsync();
        await using var connection = new MySqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        foreach (var statement in new[]
        {
            "INSERT INTO users (id, email, password_hash) VALUES (1, 'seller@example.test', 'hash'), (2, 'buyer@example.test', 'hash')",
            "INSERT INTO auctions (id, name, car_model, end_time, seller_id) VALUES (1, 'Car', 'Model', UTC_TIMESTAMP(), 1)",
            "INSERT INTO bids (amount, time, buyer_id, auction_id) VALUES (100, UTC_TIMESTAMP(), 2, 1)",
            "INSERT INTO auction_images (auction_id, image_data, content_type) VALUES (1, X'89504E470D0A1A0A', 'image/png')",
            "INSERT INTO buy_now_purchases (auction_id, buyer_id, price, purchased_at) VALUES (1, 2, 200, UTC_TIMESTAMP())"
        })
        {
            await using var command = new MySqlCommand(statement, connection);
            await command.ExecuteNonQueryAsync();
        }

        await using (var command = new MySqlCommand(
            "SELECT image_data, content_type FROM auction_images WHERE auction_id = 1", connection))
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }, (byte[])reader["image_data"]);
            Assert.Equal("image/png", reader.GetString("content_type"));
        }

        await fixture.ResetDatabaseAsync();

        foreach (var table in new[] { "buy_now_purchases", "auction_images", "bids", "auctions", "users" })
        {
            await using var command = new MySqlCommand($"SELECT COUNT(*) FROM {table}", connection);
            Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
        }
    }
}
