namespace SimpleAuctions.Rules;

public static class BidRules
{
    public static string? Validate(decimal amount, DateTimeOffset endTime, DateTimeOffset now, int sellerId, int buyerId, decimal? highestBid = null, decimal startPrice = 0)
    {
        if (highestBid is null && amount < startPrice)
        return "First bid must be at least the start price.";

        if (sellerId == buyerId)
        return "You cannot bid on your own auction.";

        if (now >= endTime)
        return "Auction has ended";

        if (decimal.Truncate(amount) != amount)
            return "Amount must be in whole kronor";

        if (amount <= 0)
            return "Amount must be greater than zero.";

        if (amount < (highestBid ?? 0) + 1000)
            return "Bid must be at least 1000 kr higher than the highest bid.";
            
        return null;
    }
}
