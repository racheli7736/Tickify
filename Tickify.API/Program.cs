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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
