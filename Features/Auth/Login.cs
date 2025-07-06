using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NoteApi.Infrastructure.Database;
using NoteApi.Infrastructure.Services;

namespace NoteApi.Features.Auth
{
    public static class Login
    {
        public record Command(string Email, string Password);

        public record Response(string Token);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Email).NotEmpty().EmailAddress();
                RuleFor(x => x.Password).NotEmpty();
            }
        }

        public class Endpoint
        {
            public static void Map(IEndpointRouteBuilder app) =>
                app.MapPost("/api/auth/login", Handle)
                 .WithTags("Auth")
                 .WithSummary("Logs in a user and returns a JWT");

            private static async Task<IResult> Handle(
                Command command,
                AppDbContext db,
                IJwtService jwtService,
                IValidator<Command> validator,
                ILogger<Endpoint> logger,
                CancellationToken ct)
            {
                var validationResult = await validator.ValidateAsync(command, ct);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == command.Email, ct);

                if (user is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
                {
                    logger.LogWarning("Failed login attempt for email: {Email}", command.Email);
                    return Results.Unauthorized();
                }

                var token = jwtService.GenerateToken(user);

                logger.LogInformation("User logged in successfully: {Email}", user.Email);

                return Results.Ok(new Response(token));
            }
        }
    }
}
