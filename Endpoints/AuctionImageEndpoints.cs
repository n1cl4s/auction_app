using SimpleAuctions.DTOs;
using SimpleAuctions.Models;
using SimpleAuctions.Repositories;

namespace SimpleAuctions.Endpoints;

public static class AuctionImageEndpoints
{
    private const int MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    public static void MapAuctionImageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auctions/{id:int}/images");
        group.MapGet("", async (
            int id,
            AuctionRepository auctions,
            AuctionImageRepository images,
            CancellationToken cancellationToken) =>
        {
            if (await auctions.GetById(id, cancellationToken) is null)
                return Results.NotFound();

            var result = await images.GetByAuction(id, cancellationToken);
            return Results.Ok(result.Select(image => ToResponse(id, image)));
        });

        group.MapGet("/{imageId:int}", async (
            int id,
            int imageId,
            AuctionRepository auctions,
            AuctionImageRepository images,
            CancellationToken cancellationToken) =>
        {
            if (await auctions.GetById(id, cancellationToken) is null)
                return Results.NotFound();

            var image = await images.GetById(id, imageId, cancellationToken);
            return image is null
                ? Results.NotFound()
                : Results.File(image.Data, image.ContentType);
        });

        group.MapPost("", async (
            int id,
            HttpContext context,
            AuctionRepository auctions,
            AuctionImageRepository images,
            CancellationToken cancellationToken) =>
        {
            int? userId = context.Session.GetInt32("UserId");
            if (userId is null)
                return Results.Unauthorized();

            var auction = await auctions.GetById(id, cancellationToken);
            if (auction is null)
                return Results.NotFound();
            if (auction.SellerId != userId.Value)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            string? contentType = context.Request.ContentType?.Split(';')[0].Trim().ToLowerInvariant();
            if (contentType is null || contentType is not ("image/png" or "image/jpeg"))
                return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            if (context.Request.ContentLength > MaxImageSizeBytes)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

            await using var imageStream = new MemoryStream();
            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await context.Request.Body.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (imageStream.Length + bytesRead > MaxImageSizeBytes)
                    return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
                await imageStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            var imageData = imageStream.ToArray();
            if (!HasExpectedSignature(imageData, contentType))
                return Results.BadRequest(new { message = "Image content does not match the supported content type." });

            var created = await images.Create(id, imageData, contentType, cancellationToken);
            if (created is null)
                return Results.NotFound();

            var response = ToResponse(id, created);
            return Results.Created(response.Url, response);
        });
    }

    private static AuctionImageResponse ToResponse(int auctionId, AuctionImageMetadata image)
    {
        var url = $"/api/auctions/{auctionId}/images/{image.Id}";
        return new AuctionImageResponse(image.Id, image.DisplayOrder, image.ContentType, url);
    }

    private static bool HasExpectedSignature(byte[] data, string contentType) =>
        contentType == "image/png"
            ? data.AsSpan().StartsWith(PngSignature)
            : contentType == "image/jpeg" && data.Length >= 3
                && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff;
}
