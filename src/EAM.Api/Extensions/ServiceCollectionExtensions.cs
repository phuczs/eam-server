using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using EAM.Api.Configuration;
using EAM.Application.Interfaces.Infrastructures;

namespace EAM.Api.Extensions;

/// <summary>
/// API-layer DI/registration helpers, kept out of Program.cs so the host stays readable.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // Request-scoped accessors implementing the Application abstractions.
        services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
        services.AddScoped<IRequestContext, HttpRequestContext>();

        // Wrap successful results in the unified ApiResponse envelope.
        services.AddControllers(o => o.Filters.Add<ApiResponseWrapperFilter>());

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddCorsSetup(this IServiceCollection services, IConfiguration config, string policyName)
    {
        services.AddCors(o => o.AddPolicy(policyName, p => p
            .WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            // Expose rate-limit headers so the SPA can read the wait time on a 429.
            .WithExposedHeaders("Retry-After", "RateLimit-Reset", "RateLimit-Remaining")));

        return services;
    }

    public static IServiceCollection AddSwaggerSetup(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "EAM Platform API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the access token without the 'Bearer ' prefix."
            });

            c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });

            var xml = Path.Combine(AppContext.BaseDirectory, "EAM.Api.xml");
            if (File.Exists(xml))
            {
                c.IncludeXmlComments(xml);
            }
        });

        return services;
    }
}
