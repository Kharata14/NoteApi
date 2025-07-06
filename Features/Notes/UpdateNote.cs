using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NoteApi.Common.Models;
using NoteApi.Infrastructure.Database;
using System.Security.Claims;

namespace NoteApi.Features.Notes
{
    public static class UpdateNote
    {
        public record Command(string Title, string Content, List<string> Tags);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
                RuleFor(x => x.Content).NotEmpty();
                RuleFor(x => x.Tags).NotNull();
            }
        }

        public class Endpoint
        {
            public static void Map(IEndpointRouteBuilder app) =>
                app.MapPut("/api/notes/{id:int}", Handle)
                 .RequireAuthorization()
                 .WithTags("Notes")
                 .WithSummary("Updates an existing note");

            private static async Task<IResult> Handle(
    int id,
    Command command,
    ClaimsPrincipal userClaims,
    AppDbContext db,
    IValidator<Command> validator,
    ILogger<Endpoint> logger,
    CancellationToken ct)
            {
                var validationResult = await validator.ValidateAsync(command, ct);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var userIdStr = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out var userId))
                {
                    return Results.Unauthorized();
                }

                var note = await db.Notes
                  .Include(n => n.NoteTags)
                  .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, ct);

                if (note is null)
                {
                    return Results.NotFound();
                }

                note.Title = command.Title;
                note.Content = command.Content;
                note.UpdatedAt = DateTime.UtcNow;

                var uniqueTagNames = command.Tags
                  .Select(t => t.ToLowerInvariant().Trim())
                  .Where(t => !string.IsNullOrEmpty(t))
                  .Distinct()
                  .ToList();

                var existingTags = await db.Tags
                  .Where(t => uniqueTagNames.Contains(t.Name))
                  .ToListAsync(ct);

                var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();
                var newTagNames = uniqueTagNames.Except(existingTagNames).ToList();
                if (newTagNames.Any())
                {
                    foreach (var newTagName in newTagNames)
                    {
                        db.Tags.Add(new Tag { Name = newTagName });
                    }
                    await db.SaveChangesAsync(ct);
                }

                var tagsToAssociate = await db.Tags
                  .Where(t => uniqueTagNames.Contains(t.Name))
                  .ToListAsync(ct);
                note.NoteTags.Clear();
                foreach (var tag in tagsToAssociate)
                {
                    note.NoteTags.Add(new NoteTag { TagId = tag.Id });
                }
                await db.SaveChangesAsync(ct);

                logger.LogInformation("Note {NoteId} updated for user {UserId}", id, userId);

                return Results.NoContent();
            }
        }
    }
}
