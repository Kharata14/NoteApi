using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NoteApi.Common.Models;
using NoteApi.Infrastructure.Database;

namespace NoteApi.Features.Auth
{
    public static class Register
    {
        public record Command(string FullName, string Email, string Password);

        public record Response(int Id, string FullName, string Email);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
                RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
                RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
            }
        }

        public class Endpoint
        {
            public static void Map(IEndpointRouteBuilder app) =>
                app.MapPost("/api/auth/register", Handle)
                 .WithTags("Auth")
                 .WithSummary("Registers a new user");

            private static async Task<IResult> Handle(
                Command command,
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

                var emailExists = await db.Users.AnyAsync(u => u.Email == command.Email, ct);
                if (emailExists)
                {
                    logger.LogWarning("Registration attempt for existing email: {Email}", command.Email);
                    return Results.Conflict("An account with this email already exists.");
                }

                var user = new User
                {
                    FullName = command.FullName,
                    Email = command.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password)
                };

                db.Users.Add(user);
                await db.SaveChangesAsync(ct);

                logger.LogInformation("New user registered: {Email}, UserId: {UserId}", user.Email, user.Id);

                var response = new Response(user.Id, user.FullName, user.Email);
                return Results.Created($"/api/users/{user.Id}", response);
            }
        }
    }

}
