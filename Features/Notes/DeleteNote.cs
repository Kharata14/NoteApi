using Microsoft.EntityFrameworkCore;
using NoteApi.Infrastructure.Database;
using System.Security.Claims;

namespace NoteApi.Features.Notes
{
    public static class DeleteNote
    {
        public class Endpoint
        {
            public static void Map(IEndpointRouteBuilder app) =>
                app.MapDelete("/api/notes/{id:int}", Handle)
                 .RequireAuthorization()
                 .WithTags("Notes")
                 .WithSummary("Soft deletes a note");

            private static async Task<IResult> Handle(
                int id,
                ClaimsPrincipal userClaims,
                AppDbContext db,
                ILogger<Endpoint> logger,
                CancellationToken ct)
            {
                var userIdStr = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out var userId))
                {
                    return Results.Unauthorized();
                }

                var note = await db.Notes
                 .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, ct);

                if (note is null)
                {
                    return Results.NotFound();
                }

                note.IsDeleted = true;
                note.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync(ct);

                logger.LogInformation("Note {NoteId} soft-deleted for user {UserId}", id, userId);

                return Results.NoContent();
            }
        }
    }

}
