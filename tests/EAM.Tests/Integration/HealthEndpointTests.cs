using System.Net;
using FluentAssertions;
using Xunit;

namespace EAM.Tests.Integration;

/// <summary>
/// Skeleton integration test — boots the API host and hits the health probe.
/// Demonstrates the WebApplicationFactory pattern for future endpoint tests.
/// </summary>
public class HealthEndpointTests : IClassFixture<EamWebAppFactory>
{
    private readonly EamWebAppFactory _factory;

    public HealthEndpointTests(EamWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200_and_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("healthy");
    }
}
