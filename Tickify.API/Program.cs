using System.Text.Json.Serialization;
using NLog.Web;
using Tickify.API.Extensions;
using Tickify.API.Middleware;
using Tickify.Data;
using Tickify.Service.Options;

var builder = WebApplication.CreateBuilder(args);

// NLog is the only logging provider; code keeps using ILogger<T>. RemoveLoggerFactoryFilter = false keeps
// the Logging:LogLevel section of appsettings in charge of the minimum levels.
builder.Logging.ClearProviders();
builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

// Fail fast on missing secrets (they live in User Secrets, never in the repo).
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
if (string.IsNullOrWhiteSpace(jwtSection["Key"]) || jwtSection["Key"]!.Length < 64)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 64 characters. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<random 64+ chars>\" --project Tickify.API");
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default")))
    throw new InvalidOperationException(
        "ConnectionStrings:Default is missing. Set it with dotnet user-secrets (see README).");

// Hosting platforms (Render, Heroku...) hand out the database as a URL: postgresql://user:password@host:port/db.
// Npgsql wants key=value pairs, so convert it. Normal key=value connection strings pass through untouched.
var rawConnection = builder.Configuration.GetConnectionString("Default")!;
if (rawConnection.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
    rawConnection.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
    var uri = new Uri(rawConnection);
    var credentials = uri.UserInfo.Split(':', 2);
    builder.Configuration["ConnectionStrings:Default"] = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
    }.ConnectionString;
}

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

builder.Services.AddSwaggerDocs();

var app = builder.Build();

// Pipeline order: errors first (so nothing below can leak an unhandled exception), then the correlation id.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseStatusCodePages();

// Create / update the database (and seed data) on startup: always in Development, and in any other environment
// only when explicitly switched on (Database__MigrateOnStartup=true, used by the Render deployment).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.EnablePersistAuthorization()); // keep the token across page reloads
}

// In production TLS is terminated by the hosting platform's proxy, so the app itself only speaks HTTP there.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors(Tickify.API.Extensions.ServiceCollectionExtensions.CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })); // used by the hosting platform's health check

app.Run();

// Makes the entry point visible to the integration tests (WebApplicationFactory<Program>).
public partial class Program { }
