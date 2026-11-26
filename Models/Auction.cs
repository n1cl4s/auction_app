namespace SimpleAuctions.Models;

public record Auction(
    int Id,
    string Name,
    DateTimeOffset EndTime,
    int SellerId,
    string? CarModel = null,
    int? ModelYear = null,
    uint? MileageKm = null,
    string? Transmission = null,
    string? FuelType = null,
    string? Description = null,
    string? Color = null,
    decimal? StartingPrice = null,
    decimal? BuyNowPrice = null);
