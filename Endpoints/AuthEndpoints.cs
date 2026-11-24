
using Microsoft.AspNetCore.Identity;
using SimpleAuctions.DTOs;
using SimpleAuctions.Models;
using SimpleAuctions.Repositories;

namespace SimpleAuctions.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth/login");

        // Log in user
        group.MapPost("", async (
            LoginDto data,
            HttpContext context,
            UserRepository repository,
            IPasswordHasher<User> hasher) =>
        {
            if (string.IsNullOrWhiteSpace(data.Email)
                || string.IsNullOrEmpty(data.Password)
                || data.Email.Length > 254
                || data.Password.Length > 128)
            {
                return Results.BadRequest(new
                {
                    message = "Email and password are required."
                });
            }

            var user = await repository.GetByEmail(
                data.Email.Trim().ToLowerInvariant());

            if (user is null ||
                hasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    data.Password) == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            context.Session.Clear();
            context.Session.SetInt32("UserId", user.Id);

            return Results.Ok(new
            {
                user = new UserResponseDto(user.Id, user.Email)
            });
        });

        // Get currently authenticated user
        group.MapGet("", async (
            HttpContext context,
            UserRepository repository) =>
        {
            int? id = context.Session.GetInt32("UserId");

            if (id is null)
                return Results.Unauthorized();

            var user = await repository.GetById(id.Value);

            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new
                {
                    authenticated = true,
                    user = new UserResponseDto(user.Id, user.Email)
                });
        });

        // Log out user and clear session
        group.MapDelete("", (HttpContext context) =>
        {
            context.Session.Clear();
            context.Response.Cookies.Delete("SimpleAuctions.Session");

            return Results.NoContent();
        });

        // Check administrator access
        app.MapGet("/api/auth/admin", async (
            HttpContext context,
            UserRepository repository) =>
        {
            int? userId = context.Session.GetInt32("UserId");

            // Return 401 if the user is not logged in
            if (userId is null)
                return Results.Unauthorized();

            var user = await repository.GetById(userId.Value);

            // Return 401 if the user no longer exists
            if (user is null)
                return Results.Unauthorized();

            // Return 403 if the user is not an administrator
            if (!user.IsAdmin)
                return Results.StatusCode(403);

            // Grant access to administrators
            return Results.Ok(new
            {
                message = "Admin access granted."
            });
        });
    }
}
