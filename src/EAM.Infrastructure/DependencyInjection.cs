using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EAM.Application.Interfaces.Infrastructures;
using EAM.Application.Interfaces.Services;
using EAM.Application.Options;
using EAM.Application.Services;
using EAM.Infrastructure.Caching;
using EAM.Infrastructure.Email;
using EAM.Infrastructure.Helper;
using EAM.Infrastructure.Logging;
using EAM.Infrastructure.Persistence;
using EAM.Infrastructure.Security;
using EAM.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EAM.Infrastructure;

/// <summary>
/// Infrastructure-layer composition root: options, storage, caching, email, security,
/// helpers, and auditing. Persistence (DbContext + repositories) is added once the
/// domain exists — see the commented block.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ── Options ──
        services.Configure<AzureAdOptions>(config.GetSection(AzureAdOptions.SectionName));
        services.Configure<SingpassOptions>(config.GetSection(SingpassOptions.SectionName));
        services.Configure<BlobStorageOptions>(config.GetSection(BlobStorageOptions.SectionName));
        services.Configure<GmailOptions>(config.GetSection(GmailOptions.SectionName));

        // Persistence
        var dbConnection = config.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=EamDb;Trusted_Connection=True;TrustServerCertificate=True;";
        services.AddDbContext<EamDbContext>(o =>
            o.UseSqlServer(dbConnection,
                sql => sql.EnableRetryOnFailure()));

        // ── Storage (BlobServiceClient is thread-safe → singleton) ──
        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

        // ── Security ──
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // ── Helpers ──
        services.AddScoped<IFileService, FileService>();

        // ── Caching ──
        services.AddMemoryCache();
        services.AddSingleton<ILookupCache, MemoryLookupCache>();

        var redisConn = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
        }
        else
        {
            // Local/dev fallback so the app still boots without Redis.
            services.AddDistributedMemoryCache();
        }
        services.AddScoped<IDistributedCacheService, RedisCacheService>();

        // ── Email ──
        // Default: log-only sender (no external credentials needed for local/dev).
        // Switch to the real Gmail sender once GmailOptions are configured:
        //   services.AddScoped<IEmailSender, GmailEmailSender>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<IEmailService, EmailService>();
        // Background dispatch alternative (requires a configured Hangfire job store):
        //   services.AddScoped<EmailService>();
        //   services.AddScoped<IEmailService, HangfireEmailService>();

        // ── Auditing ──
        services.AddScoped<IAuditWriter, AuditWriter>();

        return services;
    }
}
