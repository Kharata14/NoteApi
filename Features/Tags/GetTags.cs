using Microsoft.EntityFrameworkCore;
using NoteApi.Infrastructure.Database;

namespace NoteApi.Features.Tags
{
    public static class GetTags
    {
        public record Response(List<string> Tags);

        public class Endpoint
        {
            public static void Map(IEndpointRouteBuilder app) =>
                app.MapGet("/api/tags", Handle)
                 .RequireAuthorization()
                 .WithTags("Tags")
                 .WithSummary("Gets a list of all unique tags");

            private static async Task<IResult> Handle(
                AppDbContext db,
                CancellationToken ct)
            {
                var tags = await db.Tags
                 .AsNoTracking()
                 .OrderBy(t => t.Name)
                 .Select(t => t.Name)
                 .ToListAsync(ct);

                return Results.Ok(new Response(tags));
            }
        }
    }
}
