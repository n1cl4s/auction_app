namespace SimpleAuctions.Models;

public sealed record BuyNowPurchase(int AuctionId, int BuyerId, decimal Price, DateTimeOffset PurchasedAt);
