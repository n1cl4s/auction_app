using MySqlConnector;
using SimpleAuctions.Models;
using SimpleAuctions.Rules;

namespace SimpleAuctions.Repositories;

public class BidRepository(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("AuctionDatabase")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:AuctionDatabase.");

    public async Task<List<Bid>> GetByAuction(int auctionId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("""
            SELECT id, amount, time, buyer_id, auction_id FROM bids
            WHERE auction_id = @auction ORDER BY amount DESC, id;
            """, connection);
        command.Parameters.AddWithValue("@auction", auctionId);
        await using var reader = await command.ExecuteReaderAsync();
        List<Bid> bids = [];
        while (await reader.ReadAsync())
            bids.Add(new Bid(reader.GetInt32("id"), reader.GetDecimal("amount"),
                new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime("time"), DateTimeKind.Utc)),
                reader.GetInt32("buyer_id"), reader.GetInt32("auction_id")));
        return bids;
    }

    public async Task<(Bid? Bid, string? Error, bool Found, bool Forbidden)> Create(int auctionId, decimal amount, int buyerId)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        DateTimeOffset endTime;
        int sellerId;
        decimal startPrice;
        await using (var command = new MySqlCommand(
            "SELECT end_time, seller_id, starting_price FROM auctions WHERE id = @id;", connection))
        {
            command.Parameters.AddWithValue("@id", auctionId);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return (null, "Auction not found.", false, false);
            endTime = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime("end_time"), DateTimeKind.Utc));
            sellerId = reader.GetInt32("seller_id");
            startPrice = reader.IsDBNull(reader.GetOrdinal("starting_price")) ? 0 :
            reader.GetDecimal("starting_price");
        }
        if (sellerId == buyerId)
        return (null, "You cannot bid on your own auction.", true, true);
        await using var maxCommand = new MySqlCommand(
        "SELECT MAX(amount) FROM bids WHERE auction_id = @id;", connection);
        maxCommand.Parameters.AddWithValue("@id", auctionId);
        var maxValue = await maxCommand.ExecuteScalarAsync();
        decimal? highestBid = maxValue is DBNull ? null : (decimal)maxValue!;
        var now = DateTimeOffset.UtcNow;
        string? error = BidRules.Validate(amount, endTime, now, sellerId, buyerId, highestBid, startPrice);
        if (error is not null)
            return (null, error, true, false);

        await using var insert = new MySqlCommand("""
            INSERT INTO bids (amount, time, buyer_id, auction_id)
            VALUES (@amount, @time, @buyer, @auction);
            """, connection);
        insert.Parameters.AddWithValue("@amount", amount);
        insert.Parameters.AddWithValue("@time", now.UtcDateTime);
        insert.Parameters.AddWithValue("@buyer", buyerId);
        insert.Parameters.AddWithValue("@auction", auctionId);
        await insert.ExecuteNonQueryAsync();
        var bid = new Bid(checked((int)insert.LastInsertedId), amount, now, buyerId, auctionId);
        return (bid, null, true, false);
    }
}
