using System.Reflection;
using Microsoft.OpenApi.Models;

namespace Tickify.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Tickify API",
                Version = "v1",
                Description = "Event ticket sales. Log in via /api/auth/login, then press Authorize and paste the token."
            });

            // The "Authorize" button: the JWT goes into the Authorization: Bearer header of every request.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Paste the JWT only (the \"Bearer \" prefix is added automatically).",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                }
            });

            // Controller summaries (/// comments) become the endpoint descriptions.
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
        });

        return services;
    }
}
