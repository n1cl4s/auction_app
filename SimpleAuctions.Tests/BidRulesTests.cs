using SimpleAuctions.Rules;

namespace SimpleAuctions.Tests;

public class BidRulesTests
{
    [Fact]
    public void ZeroBidTest()
    {
        var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
        string? result = BidRules.Validate(0, endTime, endTime.AddHours(-1), 1, 2);

        Assert.NotNull(result);
    }

    [Fact]
    public void LateBidTest()
    {
    var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
    var now = endTime;

    string? result = BidRules.Validate(100, endTime, now, 1, 2);

    Assert.NotNull(result);
    }

    [Fact]
public void BidBeforeEndTimeTest()
{
    var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
    var now = endTime.AddSeconds(-1);

    string? result = BidRules.Validate(1000, endTime, now, 1, 2);

    Assert.Null(result);
}

[Fact]
public void OwnAuctionBidTest()
{
    var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
    var now = endTime.AddHours(-1);
    int sellerId = 1;
    int buyerId = 1;

    string? result = BidRules.Validate(100, endTime, now, sellerId, buyerId);

    Assert.NotNull(result);

}

[Fact]
public void OnlyIntBidTest()
{
    var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
    var now = endTime.AddHours(-1);

    string? result = BidRules.Validate(100.50m, endTime, now, 1, 2);

    Assert.NotNull(result);
}

[Fact]
public void MinRaiseTest()
{
    var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
    var now = endTime.AddHours(-1);

    string? result = BidRules.Validate(5999m, endTime, now, 1, 2, 5000m);

    Assert.NotNull(result);
}

[Fact]
public void MinRaiseOkTest()
{
    var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
    var now = endTime.AddHours(-1);

    string? result = BidRules.Validate(6000m, endTime, now, 1, 2, 5000m);

    Assert.Null(result);
}


}

