# Test database and sample sellers

The automated tests use a fresh, isolated MySQL container and create their own data. The optional [sample car script](Database/car-test-data.sql) is for a separate test database and is not run automatically.

## Load the sample cars

1. Create a test database using [car-auctions.sql](Database/car-auctions.sql), separate from any production database.
2. Connect the API to that test database and create a test user with `POST /api/users`. Note the user ID in the response.
3. Connect your SQL client to the same test database. Set the following variable in the SQL session, replacing `1` with the actual test user ID:

   ```sql
   SET @sample_seller_id = 1;
   ```

4. Run `car-test-data.sql` in that same SQL session. The variable is used only by the SQL script; it assigns that user as seller for the sample auctions.

Use a separate test user as the buyer when testing bids. Different sellers are useful for testing multiple-user scenarios, but one seller is enough for the sample data.

For auctions created through the API, the seller is taken from the logged-in session, not from `@sample_seller_id`. The API accepts and returns structured model year, mileage in kilometers, transmission, fuel type, description, color, starting price and Buy Now price. The sample descriptions retain their original Swedish mil values, converted to kilometers in the `mileage_km` column. Image operations and Buy Now purchases are not yet exposed.

See [Test.md](Test.md) for image import instructions and how to run the automated tests.
