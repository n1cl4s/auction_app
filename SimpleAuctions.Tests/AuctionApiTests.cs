using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SimpleAuctions.Tests;

[Collection(AuctionApiCollection.Name)]
public sealed class AuctionApiTests(AuctionApiFixture fixture)
{
    [Fact]
    public async Task AuthenticatedSellerCreatesAuctionThatCanBeReadBack()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        var email = TestData.UniqueEmail();
        var auctionName = $"Auction-{Guid.NewGuid():N}";
        var request = new
        {
            name = auctionName,
            endTime = DateTimeOffset.UtcNow.AddDays(1)
        };

        using var anonymousCreate = await client.PostAsJsonAsync("/api/auctions", request);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousCreate.StatusCode);

        await TestData.RegisterAndLoginAsync(client, email);
        using var create = await client.PostAsJsonAsync("/api/auctions", request);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var location = create.Headers.Location;
        Assert.NotNull(location);
        using var detail = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        Assert.Equal(auctionName, detailJson.RootElement.GetProperty("name").GetString());
        Assert.False(detailJson.RootElement.GetProperty("ended").GetBoolean());
        Assert.Equal(JsonValueKind.Null, detailJson.RootElement.GetProperty("currentPrice").ValueKind);

        using var list = await client.GetAsync("/api/auctions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Contains(listJson.RootElement.EnumerateArray(),
            auction => auction.GetProperty("name").GetString() == auctionName);
    }

    [Fact]
    public async Task AuctionCreationRejectsInvalidNameAndPastEndTime()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(client, TestData.UniqueEmail());

        using var blankName = await client.PostAsJsonAsync("/api/auctions", new
        {
            name = " ",
            endTime = DateTimeOffset.UtcNow.AddDays(1)
        });
        Assert.Equal(HttpStatusCode.BadRequest, blankName.StatusCode);

        using var pastEndTime = await client.PostAsJsonAsync("/api/auctions", new
        {
            name = "Valid name",
            endTime = DateTimeOffset.UtcNow.AddDays(-1)
        });
        Assert.Equal(HttpStatusCode.BadRequest, pastEndTime.StatusCode);
    }
}
