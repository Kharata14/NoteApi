using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NoteApi.Features.Auth;
using NoteApi.Features.Notes;
using NoteApi.Features.Tags;
using NoteApi.Infrastructure.Database;
using NoteApi.Infrastructure.Middleware;
using NoteApi.Infrastructure.Services;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
 .WriteTo.Console()
 .CreateBootstrapLogger();
Log.Information("Starting up NoteApi...");
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    builder.Services.AddSingleton<IJwtService, JwtService>();
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
    };
});

    builder.Services.AddAuthorization();

    builder.Host.UseSerilog((context, services, configuration) => configuration
     .ReadFrom.Configuration(context.Configuration)
     .ReadFrom.Services(services)
     .Enrich.FromLogContext());

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    var app = builder.Build();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapScalarApiReference();
    app.MapOpenApi();
    Register.Endpoint.Map(app);
    Login.Endpoint.Map(app);
    RefreshToken.Endpoint.Map(app);
    CreateNote.Endpoint.Map(app);
    GetNoteById.Endpoint.Map(app);
    GetNotes.Endpoint.Map(app);
    UpdateNote.Endpoint.Map(app);
    DeleteNote.Endpoint.Map(app);
    GetTags.Endpoint.Map(app);
    app.MapHealthChecks("/healthz");
    app.MapHealthChecks("/readyz", new()
    {
        Predicate = (check) => check.Tags.Contains("database")
    });
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionMiddleware>();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
