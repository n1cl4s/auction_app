# Simple Auctions

A small starting point for the **Testing, Integration, Delivery** course project. A C#/.NET 10 Minimal API with MySQL and explicit SQL queries using MySqlConnector. The structure follows [MovieAPI](https://github.com/no92one/MovieAPI) and the course guides on REST and session login.

## Getting started

You need the .NET 10 SDK and MySQL 8.0.16 or later (for example, MySQL 8.4).

1. Clone the repository and go to the project directory.
2. Run `Database/car-auctions.sql` in your SQL client to create the car-auction database and tables. This script is for a fresh database; it does not migrate existing tables.

   To reset a local development database, run `Database/reset-car-auctions.sql` first, then run `Database/car-auctions.sql`. **The reset script deletes all existing data in `simple_car_auctions`; never run it against data you need.**
3. Create a separate database account. Run this with an administrator account and choose your own password:

```sql
CREATE USER 'auctionuser'@'localhost' IDENTIFIED BY 'choose-your-own-password';
GRANT SELECT, INSERT, UPDATE, DELETE ON simple_car_auctions.* TO 'auctionuser'@'localhost';
```

Adjust the host if MySQL runs in Docker, for example.

4. Configure the local connection from the project directory:

```bash
dotnet user-secrets set "ConnectionStrings:AuctionDatabase" "Server=localhost;Port=3306;Database=simple_car_auctions;User ID=auctionuser;Password=choose-your-own-password;"
dotnet run
```

The API runs at `http://localhost:5000` with the included development profile. Configuration can also be supplied through the `ConnectionStrings__AuctionDatabase` environment variable. Do not commit real connection credentials.

## Auction images API

- `POST /api/auctions/{id}/images` stores one raw PNG or JPEG image (maximum 5 MiB). The request must be from the authenticated auction seller and use the matching image content type.
- `GET /api/auctions/{id}/images` returns public image metadata and download URLs.
- `GET /api/auctions/{id}/images/{imageId}` returns the image bytes.

See [AuctionImages.http](AuctionImages.http) for example requests. Buy Now purchase persistence is available through the repository, but its REST endpoint is intentionally left for the separate auction-rules implementation.

## Tests

The tests are in `SimpleAuctions.Tests` and use a separate MySQL 8.4 container. They require the .NET 10 SDK and a running Docker installation; no local MySQL server or test database needs to be configured.

Run the entire test suite from the project root:

```bash
dotnet test SimpleAuctions.slnx
```

Each test gets its own API client, and the test database is cleared between tests.
Read more about the tests and the solution file in [Test.md](Test.md).

## Structure

| Part | Responsibility |
| --- | --- |
| `Program.cs` | Configuration, DI, sessions and endpoint registration |
| `Models/` | User, Auction and Bid records |
| `DTOs/` | API input and user responses without password hashes |
| `Endpoints/` | HTTP, sessions, basic validation and responses |
| `Repositories/` | Parameterized SQL and mapping from MySQL |
| `Rules/BidRules.cs` | Bid rules that can be tested without a database |
| `Database/script.sql` | Database and three tables |

A bid goes from the endpoint to BidRepository, which checks that the auction exists, calls `BidRules.Validate(amount)` and saves the bid. The bid rules are intentionally incomplete: students should investigate and extend them.

## Database and rules

| Table | Columns |
| --- | --- |
| users | id, email (unik), password_hash |
| auctions | id, name, end_time, seller_id |
| bids | id, amount, time, buyer_id, auction_id |

- Auctions start immediately and must have a name and a future end time.
- The bid amount may have at most two decimal places.
- Login is required to create auctions and bids.
- All times are stored in UTC. Send `endTime` as ISO 8601 with `Z` or an explicit offset.
- Seller and buyer identities come from the session. The server sets the bid time.
- Auction status is derived from the time.
- There are no starting prices, reserve prices, hidden bids, admin roles, update/delete functions or alternative auction types.

`GET /api/auctions/{id}` also shows `ended`, `currentPrice`, `winnerId` and `bids`. The price is `null` before the first bid and the winner is `null` before the auction ends or if there are no bids. These values are not stored separately.

## Endpoints

| Method | URL | Function |
| --- | --- | --- |
| POST | /api/users | Create a user |
| GET | /api/users/{id} | View your own user (login required) |
| POST | /api/auth/login | Log in |
| GET | /api/auth/login | Check login status |
| DELETE | /api/auth/login | Log out |
| GET | /api/auctions | List auctions |
| GET | /api/auctions/{id} | View auction, bids and result |
| POST | /api/auctions | Create an auction |
| GET | /api/auctions/{id}/bids | View bids |
| POST | /api/auctions/{id}/bids | Place a bid |

Example request bodies:

```json
{ "email": "seller@example.test", "password": "DemoPassword123!" }
```

The same format is used for user creation and login.

```json
{ "name": "An old synthesizer", "endTime": "2030-01-01T12:00:00Z" }
```

## Scope

Session login follows the course's educational example using `HttpContext.Session`. Passwords are hashed with ASP.NET Core's `PasswordHasher`, and hashes are never returned to the client. Sessions are held in process memory, disappear on restart and are not shared between instances. Production mode requires HTTPS for the cookie. Production authentication, session rotation, CSRF protection and login rate limiting need to be addressed before real-world use.

Extend this starting point with testing, documentation and small functional steps according to the project assignment.
# auction_app
