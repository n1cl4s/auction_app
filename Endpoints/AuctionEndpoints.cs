using SimpleAuctions.DTOs;
using SimpleAuctions.Repositories;

namespace SimpleAuctions.Endpoints;

public static class AuctionEndpoints
{
    public static void MapAuctionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auctions");
        group.MapGet("", async (AuctionRepository repository) => Results.Ok(await repository.GetAll()));
        group.MapGet("/{id:int}", async (int id, AuctionRepository auctions, BidRepository bids) =>
        {
            var auction = await auctions.GetById(id);
            if (auction is null) return Results.NotFound();
            var history = await bids.GetByAuction(id);
            bool ended = DateTimeOffset.UtcNow >= auction.EndTime;
            var highest = history.FirstOrDefault();
            return Results.Ok(new
            {
                auction.Id, auction.Name, auction.EndTime, auction.SellerId,
                auction.CarModel, auction.ModelYear, auction.MileageKm, auction.Transmission,
                auction.FuelType, auction.Description, auction.Color, auction.StartingPrice, auction.BuyNowPrice,
                ended, currentPrice = highest?.Amount,
                winnerId = ended ? highest?.BuyerId : null,
                bids = history
            });
        });
        group.MapPost("", async (CreateAuctionDto data, HttpContext context, AuctionRepository repository) =>
        {
            int? sellerId = context.Session.GetInt32("UserId");
            if (sellerId is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(data.Name) || data.Name.Trim().Length > 200
                || data.EndTime <= DateTimeOffset.UtcNow || data.EndTime.Year < 1000)
                return Results.BadRequest(new { message = "Name (1–200 characters) and a future endTime are required." });
            if (!IsValidCarDetails(data))
                return Results.BadRequest(new { message = "Car details or prices are invalid." });
            var auction = await repository.Create(
                data.Name.Trim(), data.EndTime, sellerId.Value, data.CarModel, data.ModelYear,
                data.MileageKm, data.Transmission, data.FuelType, data.Description, data.Color,
                data.StartingPrice, data.BuyNowPrice);
            return Results.Created($"/api/auctions/{auction.Id}", auction);
        });
    }

    private static bool IsValidCarDetails(CreateAuctionDto data) =>
        data.CarModel is not { Length: > 200 }
        && data.ModelYear is not (< 0 or > ushort.MaxValue)
        && data.Transmission is not { Length: > 50 }
        && data.FuelType is not { Length: > 50 }
        && data.Color is not { Length: > 50 }
        && IsValidPrice(data.StartingPrice)
        && IsValidPrice(data.BuyNowPrice);

    private static bool IsValidPrice(decimal? price) =>
        price is null || (price >= 0 && price <= 9_999_999_999.99m && decimal.Round(price.Value, 2) == price.Value);
}
