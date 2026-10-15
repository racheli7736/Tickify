using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
