using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Tickify.API.Extensions;
using Tickify.API.Middleware;
using Tickify.Data;
using Tickify.Service.Options;

var builder = WebApplication.CreateBuilder(args);

// Fail fast on missing secrets (they live in User Secrets, never in the repo).
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
if (string.IsNullOrWhiteSpace(jwtSection["Key"]) || jwtSection["Key"]!.Length < 64)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 64 characters. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<random 64+ chars>\" --project Tickify.API");
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default")))
    throw new InvalidOperationException(
        "ConnectionStrings:Default is missing. Set it with dotnet user-secrets (see README).");

builder.Services.Configure<JwtSettings>(jwtSection);

builder.Services
    .AddDatabase(builder.Configuration)
    .AddApplicationServices()
    .AddClientCors(builder.Configuration)
    .AddJwtAuthentication(jwtSection.Get<JwtSettings>()!);

// Enums travel as readable strings ("Admin", "Available") instead of numbers.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// One error shape for every failure (status, title, detail, correlationId): controllers' Problem(),
// ModelState 400s, 401/403 status pages and the exception middleware all go through ProblemDetails.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails =
    context => context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.GetCorrelationId());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Outermost, so every log line of a request (including the Error from the exception middleware) carries the id.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Create / update the database (and seed data) automatically on startup.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
