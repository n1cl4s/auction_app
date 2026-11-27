using SimpleAuctions.Models;

namespace SimpleAuctions.Tests;

public class UserRoleTests
{
    [Fact]
    public void NewUser_ShouldNotBeAdmin()
    {
        // Arrange
        var user = new User(
            1,
            "test@example.com",
            "hashedPassword"
        );

        // Assert
        Assert.False(user.IsAdmin);
    }

    [Fact]
    public void AdminUser_ShouldBeAdmin()
    {
        // Arrange
        var user = new User(
            2,
            "admin@example.com",
            "hashedPassword",
            true
        );

        // Assert
        Assert.True(user.IsAdmin);
    }
}