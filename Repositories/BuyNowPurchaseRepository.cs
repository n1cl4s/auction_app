using MySqlConnector;
using SimpleAuctions.Models;

namespace SimpleAuctions.Repositories;

public sealed class BuyNowPurchaseRepository(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("AuctionDatabase")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:AuctionDatabase.");

    public async Task<BuyNowPurchase?> GetByAuction(int auctionId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            SELECT auction_id, buyer_id, price, purchased_at
            FROM buy_now_purchases
            WHERE auction_id = @auctionId;
            """, connection);
        command.Parameters.AddWithValue("@auctionId", auctionId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new BuyNowPurchase(
                reader.GetInt32("auction_id"),
                reader.GetInt32("buyer_id"),
                reader.GetDecimal("price"),
                new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime("purchased_at"), DateTimeKind.Utc)))
            : null;
    }

    public async Task<BuyNowPurchase> Create(
        int auctionId, int buyerId, decimal price, CancellationToken cancellationToken = default)
    {
        var purchasedAt = DateTimeOffset.UtcNow;
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            INSERT INTO buy_now_purchases (auction_id, buyer_id, price, purchased_at)
            VALUES (@auctionId, @buyerId, @price, @purchasedAt);
            """, connection);
        command.Parameters.AddWithValue("@auctionId", auctionId);
        command.Parameters.AddWithValue("@buyerId", buyerId);
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@purchasedAt", purchasedAt.UtcDateTime);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new BuyNowPurchase(auctionId, buyerId, price, purchasedAt);
    }
}
