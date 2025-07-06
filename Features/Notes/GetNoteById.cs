using Microsoft.EntityFrameworkCore;
using NoteApi.Infrastructure.Database;
using System.Security.Claims;

namespace NoteApi.Features.Notes
{
    public static class GetNoteById
    {
        public record Response(int Id, string Title, string Content, List<string> Tags);

        public class Endpoint
        {
            public static void Map(IEndpointRouteBuilder app) =>
                app.MapGet("/api/notes/{id:int}", Handle)
                 .RequireAuthorization()
                 .WithTags("Notes")
                 .WithSummary("Gets a specific note by its ID");

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
                 .AsNoTracking()
                 .Include(n => n.NoteTags)
                 .ThenInclude(nt => nt.Tag)
                 .Where(n => n.Id == id && n.UserId == userId && !n.IsDeleted)
                 .Select(n => new Response(
                        n.Id,
                        n.Title,
                        n.Content,
                        n.NoteTags.Select(nt => nt.Tag!.Name).ToList()
                    ))
                 .FirstOrDefaultAsync(ct);

                if (note is null)
                {
                    logger.LogWarning("Note {NoteId} not found for user {UserId}", id, userId);
                    return Results.NotFound();
                }

                return Results.Ok(note);
            }
        }
    }
}
