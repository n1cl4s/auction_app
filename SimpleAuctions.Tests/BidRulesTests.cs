using SimpleAuctions.Rules;

namespace SimpleAuctions.Tests;

public class BidRulesTests
{
    [Fact]
    public void ZeroBidIsRejected()
    {
        string? result = BidRules.Validate(0);

        Assert.NotNull(result);
    }
}
