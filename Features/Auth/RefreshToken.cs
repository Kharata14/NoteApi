using NoteApi.Infrastructure.Database;
using NoteApi.Infrastructure.Services;
using System.Security.Claims;

namespace NoteApi.Features.Auth
{
    public static class RefreshToken
    {
        public record Response(string Token);

        public class Endpoint
        {
            public static void Map(IEndpointRouteBuilder app) =>
                app.MapPost("/api/auth/refresh", Handle)
                 .RequireAuthorization()
                 .WithTags("Auth")
                 .WithSummary("Refreshes an existing JWT");

            private static async Task<IResult> Handle(
                ClaimsPrincipal userClaims,
                AppDbContext db,
                IJwtService jwtService,
                ILogger<Endpoint> logger,
                CancellationToken ct)
            {
                var userIdStr = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out var userId))
                {
                    return Results.Unauthorized();
                }

                var user = await db.Users.FindAsync([userId], ct);
                if (user is null)
                {
                    logger.LogWarning("Refresh token attempt for non-existent user ID: {UserId}", userId);
                    return Results.Unauthorized();
                }

                var newToken = jwtService.GenerateToken(user);

                logger.LogInformation("Token refreshed for user: {Email}", user.Email);

                return Results.Ok(new Response(newToken));
            }
        }
    }

}
