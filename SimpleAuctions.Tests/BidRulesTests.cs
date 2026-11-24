using SimpleAuctions.Rules;

namespace SimpleAuctions.Tests;

public class BidRulesTests
{
    [Fact]
    public void ZeroBidTest()
    {
        var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
        string? result = BidRules.Validate(0, endTime, endTime.AddHours(-1));

        Assert.NotNull(result);
    }

    [Fact]
    public void LateBidTest()
    {
    var endTime = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.Zero);
    var now = endTime;

    string? result = BidRules.Validate(100, endTime, now);

    Assert.NotNull(result);
    }
}

