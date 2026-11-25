namespace SimpleAuctions.DTOs;

/// <summary>Image metadata and its public download URL.</summary>
public sealed record AuctionImageResponse(int Id, uint DisplayOrder, string ContentType, string Url);
