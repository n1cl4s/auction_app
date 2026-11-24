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

## What the tests cover

The tests check behavior that is currently implemented:

1. **User creation and login:** The existing `POST /api/users` endpoint creates a user, an incorrect password is rejected, and the user can log in and log out. The test also checks that the creation response does not include the password hash.
2. **Creating and reading an auction:** An anonymous request is rejected, while a logged-in seller can create an auction. The auction can then be retrieved and appears in the list, with the expected name and no current price before the first bid.
3. **Invalid auction:** A blank name and an end time in the past each return `400 Bad Request`.
4. **Placing a bid:** A buyer places a bid that can be read back from bid history and appears as the auction's current price.
5. **Bid validation:** An anonymous request is rejected, and a bid with more than two decimal places is rejected.
6. **Test database reset:** Data in the car-auction, image, purchase, bid and user tables can be removed between tests.

The tests do not imply that registration or auction rules are complete. For example, the current bid rule only checks decimal places; rules for ended auctions and other bidding restrictions are not covered or enforced by that rule.
