using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Tickify.Core.Interfaces.Repositories;
using Tickify.Core.Interfaces.Services;
using Tickify.Data;
using Tickify.Data.Repositories;
using Tickify.Service.Mapping;
using Tickify.Service.Options;
using Tickify.Service.Security;
using Tickify.Service.Services;

namespace Tickify.API.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "Client";

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration) =>
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Scoped: these depend on the (scoped) DbContext.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISeatRepository, SeatRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICategoryService, CategoryService>();

        // Maps entities to DTOs; the profiles live in the Service assembly.
        services.AddAutoMapper(cfg => { }, typeof(EventProfile).Assembly);

        // Singleton is safe here: they are stateless and do not depend on any scoped service.
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }

    public static IServiceCollection AddClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        return services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtSettings settings)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = JwtTokenValidation.CreateParameters(settings));

        services.AddAuthorization();
        return services;
    }
}
