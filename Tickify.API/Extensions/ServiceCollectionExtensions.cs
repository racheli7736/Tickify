using Microsoft.AspNetCore.Authentication.JwtBearer;
using Tickify.Service.Options;
using Tickify.Service.Security;

namespace Tickify.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtSettings settings)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = JwtTokenValidation.CreateParameters(settings));

        services.AddAuthorization();
        return services;
    }
}
