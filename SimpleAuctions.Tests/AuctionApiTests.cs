using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MySqlConnector;

namespace SimpleAuctions.Tests;

[Collection(AuctionApiCollection.Name)]
public sealed class AuctionApiTests(AuctionApiFixture fixture)
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
    private static readonly byte[] JpegSignature = [0xff, 0xd8, 0xff];

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
            endTime = DateTimeOffset.UtcNow.AddDays(1), startingPrice = 1000m
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
            startingPrice = 2099900m,
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
    public async Task AuctionOwnerCanUploadAndRetrieveImage()
    {
        await fixture.ResetDatabaseAsync();
        using var owner = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(owner, TestData.UniqueEmail());
        using var createAuction = await owner.PostAsJsonAsync("/api/auctions", new
        {
            name = "Image test car",
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            startingPrice = 1000m
        });
        Assert.Equal(HttpStatusCode.Created, createAuction.StatusCode);
        using var auctionJson = JsonDocument.Parse(await createAuction.Content.ReadAsStringAsync());
        var auctionId = auctionJson.RootElement.GetProperty("id").GetInt32();
        var imagePath = $"/api/auctions/{auctionId}/images";

        using var anonymous = fixture.CreateClient();
        using var anonymousContent = new ByteArrayContent(PngSignature);
        anonymousContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var anonymousUpload = await anonymous.PostAsync(imagePath, anonymousContent);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousUpload.StatusCode);

        using var otherUser = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(otherUser, TestData.UniqueEmail());
        using var otherUserContent = new ByteArrayContent(PngSignature);
        otherUserContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var forbiddenUpload = await otherUser.PostAsync(imagePath, otherUserContent);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpload.StatusCode);

        using var content = new ByteArrayContent(PngSignature);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var upload = await owner.PostAsync(imagePath, content);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var imageResponse = await upload.Content.ReadFromJsonAsync<JsonElement>();
        var imageId = imageResponse.GetProperty("id").GetInt32();
        Assert.Equal($"{imagePath}/{imageId}", imageResponse.GetProperty("url").GetString());

        using var jpegContent = new ByteArrayContent(JpegSignature);
        jpegContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        using var jpegUpload = await owner.PostAsync(imagePath, jpegContent);
        Assert.Equal(HttpStatusCode.Created, jpegUpload.StatusCode);
        var jpegResponse = await jpegUpload.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1u, jpegResponse.GetProperty("displayOrder").GetUInt32());

        using var list = await owner.GetAsync(imagePath);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var imageList = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, imageList.GetArrayLength());

        using var download = await owner.GetAsync($"{imagePath}/{imageId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngSignature, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task AuctionImageUploadRejectsUnsupportedContentType()
    {
        await fixture.ResetDatabaseAsync();
        using var owner = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(owner, TestData.UniqueEmail());
        using var createAuction = await owner.PostAsJsonAsync("/api/auctions", new
        {
            name = "Image test car",
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            startingPrice = 1000m
        });
        Assert.Equal(HttpStatusCode.Created, createAuction.StatusCode);
        using var auctionJson = JsonDocument.Parse(await createAuction.Content.ReadAsStringAsync());
        var auctionId = auctionJson.RootElement.GetProperty("id").GetInt32();
        using var content = new ByteArrayContent(PngSignature);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/gif");

        using var response = await owner.PostAsync($"/api/auctions/{auctionId}/images", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task AuctionImageUploadRejectsOversizedContent()
    {
        await fixture.ResetDatabaseAsync();
        using var owner = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(owner, TestData.UniqueEmail());
        using var createAuction = await owner.PostAsJsonAsync("/api/auctions", new
        {
            name = "Image test car",
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            startingPrice = 1000m
        });
        Assert.Equal(HttpStatusCode.Created, createAuction.StatusCode);
        using var auctionJson = JsonDocument.Parse(await createAuction.Content.ReadAsStringAsync());
        var auctionId = auctionJson.RootElement.GetProperty("id").GetInt32();
        using var content = new ByteArrayContent(new byte[5 * 1024 * 1024 + 1]);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        using var response = await owner.PostAsync($"/api/auctions/{auctionId}/images", content);

        Assert.Equal((HttpStatusCode)413, response.StatusCode);
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
            endTime = DateTimeOffset.UtcNow.AddDays(1), startingPrice = 1000m
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

    [Fact]
    public async Task LowStartPriceTest()
    {
    await fixture.ResetDatabaseAsync();
    using var client = fixture.CreateClient();
    await TestData.RegisterAndLoginAsync(client, TestData.UniqueEmail());

    using var create = await client.PostAsJsonAsync("/api/auctions", new
    {
        name = "Volvo 240",
        endTime = DateTimeOffset.UtcNow.AddDays(1),
        startingPrice = 999m
    });

    Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task StartPriceOkApiTest()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(client, TestData.UniqueEmail());

        using var create = await client.PostAsJsonAsync("/api/auctions", new
        {
            name = "Volvo 240",
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            startingPrice = 1000m
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }
    
    [Fact]
    public async Task DecimalStartPriceTest()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(client, TestData.UniqueEmail());

        using var create = await client.PostAsJsonAsync("/api/auctions", new
        {
            name = "Volvo 240",
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            startingPrice = 1000.50m
        });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task LowBuyNowTest()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(client, TestData.UniqueEmail());

        using var create = await client.PostAsJsonAsync("/api/auctions", new
        {
            name = "Volvo V70",
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            startingPrice = 5000m,
            buyNowPrice = 4000m
        });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }
    
    [Fact]
    public async Task BuyNowPriceIsSavedTest()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(client, TestData.UniqueEmail());

        using var create = await client.PostAsJsonAsync("/api/auctions", new
        {
            name = "Volvo V70",
            endTime = DateTimeOffset.UtcNow.AddDays(1),
            startingPrice = 5000m,
            buyNowPrice = 8000m
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        await using var connection = new MySqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand("SELECT buy_now_price FROM auctions;", connection);
        var saved = await command.ExecuteScalarAsync();

        Assert.Equal<object?>(8000m, saved);
    }

    [Fact]
    public async Task MissingStartPriceTest()
    {
        await fixture.ResetDatabaseAsync();
        using var client = fixture.CreateClient();
        await TestData.RegisterAndLoginAsync(client, TestData.UniqueEmail());

        using var create = await client.PostAsJsonAsync("/api/auctions", new
        {
            name = "Volvo 240",
            endTime = DateTimeOffset.UtcNow.AddDays(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }
}
