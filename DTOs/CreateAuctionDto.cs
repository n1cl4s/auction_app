namespace SimpleAuctions.DTOs;

public record CreateAuctionDto(
    string Name,
    DateTimeOffset EndTime,
    string? CarModel = null,
    int? ModelYear = null,
    uint? MileageKm = null,
    string? Transmission = null,
    string? FuelType = null,
    string? Description = null,
    string? Color = null,
    decimal? StartingPrice = null,
    decimal? BuyNowPrice = null);
