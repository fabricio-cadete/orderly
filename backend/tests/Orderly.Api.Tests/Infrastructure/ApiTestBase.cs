using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Orderly.Api.Endpoints;

namespace Orderly.Api.Tests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract partial class ApiTestBase(OrderlyApiFactory factory)
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected OrderlyApiFactory Factory { get; } = factory;

    protected HttpClient Anonymous() => Factory.CreateClient();

    protected static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    /// <summary>Faz o login por link mágico e devolve a resposta de autenticação.</summary>
    protected async Task<AuthResponse> MagicLinkLoginAsync(string email)
    {
        using var client = Anonymous();
        var requested = await client.PostAsJsonAsync("/auth/magic-link", new { email });
        Assert.Equal(System.Net.HttpStatusCode.Accepted, requested.StatusCode);

        var verify = await client.PostAsJsonAsync("/auth/magic-link/verify", new { token = TokenFromLastEmail(email) });
        verify.EnsureSuccessStatusCode();
        return (await verify.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    protected async Task<HttpClient> SignedInClientAsync(string? email = null)
    {
        var auth = await MagicLinkLoginAsync(email ?? NewEmail());
        return WithToken(auth.AccessToken);
    }

    protected HttpClient WithToken(string accessToken)
    {
        var client = Anonymous();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    protected string TokenFromLastEmail(string email)
    {
        var match = TokenRegex().Match(Factory.Emails.LastTo(email).TextBody);
        Assert.True(match.Success, "O e-mail não trouxe o link com token.");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    [GeneratedRegex(@"token=([A-Za-z0-9_\-%]+)")]
    private static partial Regex TokenRegex();
}
