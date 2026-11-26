using MySqlConnector;
using SimpleAuctions.Models;

namespace SimpleAuctions.Repositories;

public class AuctionRepository(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("AuctionDatabase")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:AuctionDatabase.");

    public async Task<List<Auction>> GetAll()
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand(
            """
            SELECT id, name, end_time, seller_id, car_model, model_year, mileage_km,
                   transmission, fuel_type, description, color, starting_price, buy_now_price
            FROM auctions ORDER BY id;
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        List<Auction> auctions = [];
        while (await reader.ReadAsync())
            auctions.Add(ReadAuction(reader));
        return auctions;
    }

    public async Task<Auction?> GetById(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(
            """
            SELECT id, name, end_time, seller_id, car_model, model_year, mileage_km,
                   transmission, fuel_type, description, color, starting_price, buy_now_price
            FROM auctions WHERE id = @id;
            """, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadAuction(reader) : null;
    }

    public async Task<Auction> Create(
        string name,
        DateTimeOffset endTime,
        int sellerId,
        string? carModel,
        int? modelYear,
        uint? mileageKm,
        string? transmission,
        string? fuelType,
        string? description,
        string? color,
        decimal? startingPrice,
        decimal? buyNowPrice)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("""
            INSERT INTO auctions (
                name, end_time, seller_id, car_model, model_year, mileage_km,
                transmission, fuel_type, description, color, starting_price, buy_now_price
            ) VALUES (
                @name, @end, @seller, @carModel, @modelYear, @mileageKm,
                @transmission, @fuelType, @description, @color, @startingPrice, @buyNowPrice
            );
            """, connection);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@end", endTime.UtcDateTime);
        command.Parameters.AddWithValue("@seller", sellerId);
        command.Parameters.AddWithValue("@carModel", (object?)carModel ?? DBNull.Value);
        command.Parameters.AddWithValue("@modelYear", (object?)modelYear ?? DBNull.Value);
        command.Parameters.AddWithValue("@mileageKm", (object?)mileageKm ?? DBNull.Value);
        command.Parameters.AddWithValue("@transmission", (object?)transmission ?? DBNull.Value);
        command.Parameters.AddWithValue("@fuelType", (object?)fuelType ?? DBNull.Value);
        command.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);
        command.Parameters.AddWithValue("@color", (object?)color ?? DBNull.Value);
        command.Parameters.AddWithValue("@startingPrice", (object?)startingPrice ?? DBNull.Value);
        command.Parameters.AddWithValue("@buyNowPrice", (object?)buyNowPrice ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
        return new Auction(
            checked((int)command.LastInsertedId), name, endTime.ToUniversalTime(), sellerId,
            carModel, modelYear, mileageKm, transmission, fuelType, description, color, startingPrice, buyNowPrice);
    }

    private static Auction ReadAuction(MySqlDataReader reader) => new(
        reader.GetInt32("id"), reader.GetString("name"),
        new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime("end_time"), DateTimeKind.Utc)),
        reader.GetInt32("seller_id"),
        ReadNullableString(reader, "car_model"),
        ReadNullableInt32(reader, "model_year"),
        ReadNullableUInt32(reader, "mileage_km"),
        ReadNullableString(reader, "transmission"),
        ReadNullableString(reader, "fuel_type"),
        ReadNullableString(reader, "description"),
        ReadNullableString(reader, "color"),
        ReadNullableDecimal(reader, "starting_price"),
        ReadNullableDecimal(reader, "buy_now_price"));

    private static string? ReadNullableString(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetString(column);

    private static int? ReadNullableInt32(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetInt32(column);

    private static uint? ReadNullableUInt32(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetUInt32(column);

    private static decimal? ReadNullableDecimal(MySqlDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetDecimal(column);
}
