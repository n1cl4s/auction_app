using MySqlConnector;
using SimpleAuctions.Models;

namespace SimpleAuctions.Repositories;

public sealed class AuctionImageRepository(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("AuctionDatabase")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:AuctionDatabase.");

    public async Task<List<AuctionImageMetadata>> GetByAuction(int auctionId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            SELECT id, display_order, content_type
            FROM auction_images
            WHERE auction_id = @auctionId
            ORDER BY display_order, id;
            """, connection);
        command.Parameters.AddWithValue("@auctionId", auctionId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        List<AuctionImageMetadata> images = [];
        while (await reader.ReadAsync(cancellationToken))
            images.Add(new AuctionImageMetadata(
                reader.GetInt32("id"), reader.GetUInt32("display_order"), reader.GetString("content_type")));
        return images;
    }

    public async Task<AuctionImageContent?> GetById(
        int auctionId, int imageId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            SELECT image_data, content_type
            FROM auction_images
            WHERE auction_id = @auctionId AND id = @imageId;
            """, connection);
        command.Parameters.AddWithValue("@auctionId", auctionId);
        command.Parameters.AddWithValue("@imageId", imageId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new AuctionImageContent((byte[])reader["image_data"], reader.GetString("content_type"))
            : null;
    }

    public async Task<AuctionImageMetadata?> Create(
        int auctionId, byte[] imageData, string contentType, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var lockAuction = new MySqlCommand(
            "SELECT id FROM auctions WHERE id = @auctionId FOR UPDATE;", connection, transaction))
        {
            lockAuction.Parameters.AddWithValue("@auctionId", auctionId);
            if (await lockAuction.ExecuteScalarAsync(cancellationToken) is null)
                return null;
        }

        uint displayOrder;
        await using (var orderCommand = new MySqlCommand("""
            SELECT COALESCE(MAX(display_order) + 1, 0)
            FROM auction_images
            WHERE auction_id = @auctionId;
            """, connection, transaction))
        {
            orderCommand.Parameters.AddWithValue("@auctionId", auctionId);
            displayOrder = Convert.ToUInt32(await orderCommand.ExecuteScalarAsync(cancellationToken));
        }

        await using var insert = new MySqlCommand("""
            INSERT INTO auction_images (auction_id, image_data, content_type, display_order)
            VALUES (@auctionId, @imageData, @contentType, @displayOrder);
            """, connection, transaction);
        insert.Parameters.AddWithValue("@auctionId", auctionId);
        insert.Parameters.Add("@imageData", MySqlDbType.LongBlob).Value = imageData;
        insert.Parameters.AddWithValue("@contentType", contentType);
        insert.Parameters.AddWithValue("@displayOrder", displayOrder);
        await insert.ExecuteNonQueryAsync(cancellationToken);
        var image = new AuctionImageMetadata(
            checked((int)insert.LastInsertedId), displayOrder, contentType);
        await transaction.CommitAsync(cancellationToken);
        return image;
    }
}
