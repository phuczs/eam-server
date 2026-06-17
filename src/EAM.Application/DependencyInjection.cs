using AutoMapper;
using EAM.Application.Interfaces.Services;
using EAM.Application.Mappings;
using EAM.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace EAM.Application;

/// <summary>
/// Application-layer composition root. Wires use-case services, validators, and mapping.
/// Registrations are intentionally minimal in the skeleton — add services as they are built.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {


        // Register application services here once they are implemented, e.g.:
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPaymentService,PaymentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IMapper>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);

            return config.CreateMapper();
        });
        // services.AddScoped<IAuthService, AuthService>();

        // FluentValidation (when validators are added):
        // services.AddValidatorsFromAssemblyContaining<SomeRequestValidator>();

        return services;
    }
}
