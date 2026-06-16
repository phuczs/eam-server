using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EAM.Tests.Integration;

/// <summary>
/// Boots the real <c>Program.cs</c> under the "Testing" environment and supplies
/// in-memory Jwt config, so integration tests run with no external dependencies.
/// </summary>
public class EamWebAppFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "eam",
                ["Jwt:Audience"] = "eam-clients",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:SigningKey"] = "test-signing-key-0123456789abcdef0123456789abcdef0123456789ab"
            });
        });

        return base.CreateHost(builder);
    }
}
