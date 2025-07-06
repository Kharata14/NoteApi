using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteApi.Infrastructure.Database;
using System.Security.Claims;

namespace NoteApi.Features.Notes
{
    public static class GetNotes
    {
        public record Query(
            [FromQuery(Name = "page")] int PageNumber = 1,
            [FromQuery(Name = "size")] int PageSize = 10,
            [FromQuery(Name = "search")] string? SearchTerm = null,
            [FromQuery(Name = "tags")] string? Tags = null);

        public record NoteSummary(int Id, string Title, List<string> Tags, DateTime UpdatedAt);

        public record Response(List<NoteSummary> Notes, int TotalCount, int PageNumber, int PageSize);

        public class Endpoint
        {
            public static void Map(IEndpointRouteBuilder app) =>
                app.MapGet("/api/notes", Handle)
                 .RequireAuthorization()
                 .WithTags("Notes")
                 .WithSummary("Gets a paginated list of notes with search and filtering");

            private static async Task<IResult> Handle(
                [AsParameters] Query query,
                ClaimsPrincipal userClaims,
                AppDbContext db,
                CancellationToken ct)
            {
                var userIdStr = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out var userId))
                {
                    return Results.Unauthorized();
                }

                var notesQuery = db.Notes
                 .AsNoTracking()
                 .Where(n => n.UserId == userId && !n.IsDeleted);

                if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                {
                    notesQuery = notesQuery.Where(n =>
                        EF.Functions.ILike(n.Title, $"%{query.SearchTerm}%") ||
                        EF.Functions.ILike(n.Content, $"%{query.SearchTerm}%"));
                }

                if (!string.IsNullOrWhiteSpace(query.Tags))
                {
                    var tagList = query.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (tagList.Length > 0)
                    {
                        notesQuery = notesQuery.Where(n => n.NoteTags.Count(nt => tagList.Contains(nt.Tag!.Name)) == tagList.Length);
                    }
                }

                var totalCount = await notesQuery.CountAsync(ct);

                var notes = await notesQuery
                 .OrderByDescending(n => n.UpdatedAt)
                 .Skip((query.PageNumber - 1) * query.PageSize)
                 .Take(query.PageSize)
                 .Select(n => new NoteSummary(
                        n.Id,
                        n.Title,
                        n.NoteTags.Select(nt => nt.Tag!.Name).ToList(),
                        n.UpdatedAt
                    ))
                 .ToListAsync(ct);

                var response = new Response(notes, totalCount, query.PageNumber, query.PageSize);
                return Results.Ok(response);
            }
        }
    }
}
