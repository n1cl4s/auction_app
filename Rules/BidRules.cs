namespace SimpleAuctions.Rules;

public static class BidRules
{
    public static string? Validate(decimal amount, DateTimeOffset endTime, DateTimeOffset now, int sellerId, int buyerId)
    {
        if (sellerId == buyerId)
        return "You cannot bid on your own auction.";

        if (now >= endTime)
        return "Auction has ended";

        if (decimal.Truncate(amount) != amount)
            return "Amount must be in whole kronor";

        if (amount <= 0)
            return "Amount must be greater than zero.";
        return null;
    }
}
