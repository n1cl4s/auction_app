using SimpleAuctions.Models;

namespace SimpleAuctions.Tests;

public class UserRoleTests
{
    [Fact]
    public void NewUser_ShouldNotBeAdmin()
    {
        var user = new User(
            1,
            "test@example.com",
            "hashedPassword"
        );

        Assert.False(user.IsAdmin);
    }
}