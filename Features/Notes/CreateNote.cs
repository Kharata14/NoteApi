using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NoteApi.Common.Models;
using NoteApi.Infrastructure.Database;
using System.Security.Claims;

namespace NoteApi.Features.Notes
{
    public static class CreateNote
    {
        public record Command(string Title, string Content, List<string> Tags);

        public record Response(int Id, string Title, string Content, List<string> Tags, DateTime CreatedAt);

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
                app.MapPost("/api/notes", Handle)
                 .RequireAuthorization()
                 .WithTags("Notes")
                 .WithSummary("Creates a new note");

            private static async Task<IResult> Handle(
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

                var note = new Note
                {
                    UserId = userId,
                    Title = command.Title,
                    Content = command.Content
                };

                if (command.Tags.Any())
                {
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

                    foreach (var tag in tagsToAssociate)
                    {
                        note.NoteTags.Add(new NoteTag { TagId = tag.Id });
                    }
                }

                db.Notes.Add(note);
                await db.SaveChangesAsync(ct);

                logger.LogInformation("Note created with ID {NoteId} for user {UserId}", note.Id, userId);
                var responseTags = await db.NoteTags
                   .Where(nt => nt.NoteId == note.Id)
                   .Include(nt => nt.Tag)
                   .Select(nt => nt.Tag!.Name)
                   .ToListAsync(ct);

                var response = new Response(note.Id, note.Title, note.Content, responseTags, note.CreatedAt);
                return Results.Created($"/api/notes/{note.Id}", response);
            }
        }
    }
}
