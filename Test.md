# Tests

## What is an `.slnx` file?

An `.slnx` file is a .NET solution file in XML format. It groups related projects but does not contain their source code. It is a newer alternative to the older `.sln` format.

[SimpleAuctions.slnx](SimpleAuctions.slnx) includes two projects:

- [SimpleAuctions.csproj](SimpleAuctions.csproj): the API.
- [SimpleAuctions.Tests.csproj](SimpleAuctions.Tests/SimpleAuctions.Tests.csproj): the test project, which references the API project.

The command `dotnet test SimpleAuctions.slnx` builds the projects and runs the tests in the solution.

## Running the tests

You need the .NET 10 SDK and a working Docker installation. Start Docker, then run this command in Bash from the project root:

```bash
dotnet test SimpleAuctions.slnx
```

The integration tests use xUnit and Testcontainers. They start a separate MySQL 8.4 container; Docker may need to download the image the first time. The empty test database is created using [Database/car-auctions.sql](Database/car-auctions.sql). The tests call the API through a test server and do not use your regular database.

The tests share the container but do not run in parallel with one another. The test database is cleared before each test, and the container is removed when the test run finishes.

## Optional sample cars

[DatabaseTest.md](DatabaseTest.md) explains how to set up a test seller and load the sample data.

[Database/car-test-data.sql](Database/car-test-data.sql) contains eleven sample auctions. It is not loaded by the automated tests. Run it only against a separate, isolated test database with the car-auction schema, never against a production database.

In your SQL client, connect to the test database and select an existing test user's ID. In the same SQL session, set the variable below, replacing `1` with that ID, then run `car-test-data.sql`:

```sql
SET @sample_seller_id = 1;
```

The script requires that user to exist and adds eleven rows each time it runs. The auctions end 30 days after the script is run. Each description includes model year, mileage, transmission and fuel; previously unspecified details are marked as examples. The supplied prices are stored as starting prices, and the Morris Minor has an example starting price. Buy Now prices remain empty.

### Optional sample images

The supplied [Ferrari image](Database/sample-images/ferrari-812-superfast.png), [Mercedes image](Database/sample-images/mercedes-benz-w128.png), [DeSoto image](Database/sample-images/desoto-firesweep.jpg), [Buick image](Database/sample-images/buick-special.jpg), [Pontiac image](Database/sample-images/pontiac-streamliner.jpg), [Cadillac image](Database/sample-images/cadillac-series-62.jpg), [Dodge image](Database/sample-images/dodge.jpg), [Plymouth image](Database/sample-images/plymouth-fury.jpg), [Chevrolet image](Database/sample-images/chevrolet-impala-sport.jpg), [Ford Thunderbird image](Database/sample-images/ford-thunderbird.jpg), and [Morris Minor image](Database/sample-images/morris-minor.jpg) can be stored in MySQL with [car-test-image.sql](Database/car-test-image.sql). After creating the sample auctions, run the following in the same SQL session connected to the isolated test database:

1. Find the appropriate auction ID in `auctions` and set `@sample_auction_id` to that ID.
2. Make the image available to the MySQL server and set `@sample_image_server_path` to its absolute server-side path.
3. Set `@sample_image_content_type` to `image/png` for the Ferrari or Mercedes, or `image/jpeg` for the other listed images.
4. Run `car-test-image.sql`.

Repeat these steps for each image. MySQL's `LOAD_FILE` requires the server to be able to read the file, the account to have the `FILE` privilege, and the path to meet the server's `secure_file_priv` restriction. A database in Docker cannot automatically see files on the host. A missing or unreadable file causes the insert to fail. The image script adds one row each time it runs.

## What the tests cover

The tests check behavior that is currently implemented:

1. **User creation and login:** The existing `POST /api/users` endpoint creates a user, an incorrect password is rejected, and the user can log in and log out. The test also checks that the creation response does not include the password hash.
2. **Creating and reading an auction:** An anonymous request is rejected, while a logged-in seller can create an auction. The auction can then be retrieved and appears in the list, with the expected name and no current price before the first bid.
3. **Invalid auction:** A blank name and an end time in the past each return `400 Bad Request`.
4. **Placing a bid:** A buyer places a bid that can be read back from bid history and appears as the auction's current price.
5. **Bid validation:** An anonymous request is rejected, and a bid with more than two decimal places is rejected.

6. **Car auction fields:** Model year, mileage, transmission, fuel type, description, color and prices are stored and returned by the API.
7. **Auction images:** The authenticated seller can upload an image; the public image list and download endpoint return its metadata and bytes. Anonymous uploads and unsupported content types are rejected.
8. **Buy Now persistence:** The repository can store and retrieve a purchase record. Purchase eligibility rules and a public REST endpoint are not implemented here.
9. **Test database reset:** Data in the car-auction, image, purchase, bid and user tables can be removed between tests.

The tests do not imply that registration or auction rules are complete. For example, the current bid rule only checks decimal places; rules for ended auctions and other bidding restrictions are not covered or enforced by that rule.
