using Microsoft.Extensions.DependencyInjection;

namespace EAM.Application;

/// <summary>
/// Application-layer composition root. Wires use-case services, validators, and mapping.
/// Registrations are intentionally minimal in the skeleton — add services as they are built.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper: register the profile once you start adding maps, e.g.:
        // services.AddAutoMapper(cfg => { }, typeof(EAM.Application.Mappings.MappingProfile));
        // (Left unregistered in the skeleton so the host boots with zero configured maps.)

        // Register application services here once they are implemented, e.g.:
        // services.AddScoped<IUserService, UserService>();
        // services.AddScoped<IAuthService, AuthService>();

        // FluentValidation (when validators are added):
        // services.AddValidatorsFromAssemblyContaining<SomeRequestValidator>();

        return services;
    }
}
