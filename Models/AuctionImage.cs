namespace SimpleAuctions.Models;

public sealed record AuctionImageMetadata(int Id, uint DisplayOrder, string ContentType);

public sealed record AuctionImageContent(byte[] Data, string ContentType);
