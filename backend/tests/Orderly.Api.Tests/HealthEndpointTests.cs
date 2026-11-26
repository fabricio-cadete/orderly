using System.Net;
using Orderly.Api.Tests.Infrastructure;

namespace Orderly.Api.Tests;

public sealed class HealthEndpointTests(OrderlyApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Health_returns_ok()
    {
        using var client = Anonymous();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
