using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SimpleAuctions.Tests;

[Collection(AuctionApiCollection.Name)]
public sealed class BidApiTests(AuctionApiFixture fixture)
{
    [Fact]
    public async Task BuyerPlacesBidThatIsPersistedAndVisibleInAuctionHistory()
    {
        await fixture.ResetDatabaseAsync();
        using var seller = fixture.CreateClient();
        using var buyer = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(seller, TestData.UniqueEmail());

        var auctionResponse = await seller.PostAsJsonAsync("/api/auctions", new
        {
            name = $"Auction-{Guid.NewGuid():N}",
            endTime = DateTimeOffset.UtcNow.AddDays(1)
        });
        Assert.Equal(HttpStatusCode.Created, auctionResponse.StatusCode);
        var auctionUrl = auctionResponse.Headers.Location!.ToString();
        var auctionId = int.Parse(auctionUrl.Split('/').Last());
        await TestData.RegisterAndLoginAsync(buyer, TestData.UniqueEmail());

        using var bid = await buyer.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", new { amount = 25.50m });
        Assert.Equal(HttpStatusCode.Created, bid.StatusCode);

        using var bidHistory = await buyer.GetAsync($"/api/auctions/{auctionId}/bids");
        Assert.Equal(HttpStatusCode.OK, bidHistory.StatusCode);
        using var historyJson = JsonDocument.Parse(await bidHistory.Content.ReadAsStringAsync());
        var savedBid = Assert.Single(historyJson.RootElement.EnumerateArray());
        Assert.Equal(25.50m, savedBid.GetProperty("amount").GetDecimal());

        using var auction = await buyer.GetAsync(auctionUrl);
        using var auctionJson = JsonDocument.Parse(await auction.Content.ReadAsStringAsync());
        Assert.Equal(25.50m, auctionJson.RootElement.GetProperty("currentPrice").GetDecimal());
    }

    [Fact]
    public async Task BidRequiresLoginAndAcceptsOnlyTwoDecimalPlaces()
    {
        await fixture.ResetDatabaseAsync();
        using var seller = fixture.CreateClient();
        using var buyer = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(seller, TestData.UniqueEmail());

        using var create = await seller.PostAsJsonAsync("/api/auctions", new
        {
            name = $"Auction-{Guid.NewGuid():N}",
            endTime = DateTimeOffset.UtcNow.AddDays(1)
        });
        var auctionId = int.Parse(create.Headers.Location!.ToString().Split('/').Last());

        using var anonymousBid = await buyer.PostAsJsonAsync(
            $"/api/auctions/{auctionId}/bids", new { amount = 12.34m });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousBid.StatusCode);

        await TestData.RegisterAndLoginAsync(buyer, TestData.UniqueEmail());
        using var invalidBid = await buyer.PostAsJsonAsync(
            $"/api/auctions/{auctionId}/bids", new { amount = 12.345m });
        Assert.Equal(HttpStatusCode.BadRequest, invalidBid.StatusCode);
    }
}
