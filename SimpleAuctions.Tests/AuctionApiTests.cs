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
    public async Task CarAuctionFieldsArePersistedAndReturned()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(client, TestData.UniqueEmail());
        var auctionName = $"Car-{Guid.NewGuid():N}";
        var request = new
        {
            name = auctionName,
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            carModel = "Mercedes-Benz W128 - 220 SE Cabriolet",
            modelYear = 1960,
            mileageKm = 2990u,
            transmission = "manual",
            fuelType = "petrol",
            description = "Example car listing",
            color = "silver",
            startingPrice = 2099900.50m,
            buyNowPrice = 2500000m
        };

        using var create = await client.PostAsJsonAsync("/api/auctions", request);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var location = create.Headers.Location;
        Assert.NotNull(location);
        using var detail = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        var detailAuction = detailJson.RootElement;
        Assert.Equal(request.carModel, detailAuction.GetProperty("carModel").GetString());
        Assert.Equal(request.modelYear, detailAuction.GetProperty("modelYear").GetInt32());
        Assert.Equal(request.mileageKm, detailAuction.GetProperty("mileageKm").GetUInt32());
        Assert.Equal(request.transmission, detailAuction.GetProperty("transmission").GetString());
        Assert.Equal(request.fuelType, detailAuction.GetProperty("fuelType").GetString());
        Assert.Equal(request.description, detailAuction.GetProperty("description").GetString());
        Assert.Equal(request.color, detailAuction.GetProperty("color").GetString());
        Assert.Equal(request.startingPrice, detailAuction.GetProperty("startingPrice").GetDecimal());
        Assert.Equal(request.buyNowPrice, detailAuction.GetProperty("buyNowPrice").GetDecimal());

        using var list = await client.GetAsync("/api/auctions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listJson = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var listedAuction = Assert.Single(listJson.RootElement.EnumerateArray(),
            auction => auction.GetProperty("name").GetString() == auctionName);
        Assert.Equal(request.mileageKm, listedAuction.GetProperty("mileageKm").GetUInt32());
        Assert.Equal(request.fuelType, listedAuction.GetProperty("fuelType").GetString());
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

        using var negativeStartingPrice = await client.PostAsJsonAsync("/api/auctions", new
        {
            name = "Valid name",
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            startingPrice = -1m
        });
        Assert.Equal(HttpStatusCode.BadRequest, negativeStartingPrice.StatusCode);
    }
}
