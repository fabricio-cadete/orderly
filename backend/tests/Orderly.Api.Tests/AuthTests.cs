using System.Net;
using System.Net.Http.Json;
using Orderly.Api.Endpoints;
using Orderly.Api.Tests.Infrastructure;

namespace Orderly.Api.Tests;

public sealed class AuthTests(OrderlyApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Magic_link_signs_in_and_me_returns_the_user()
    {
        var email = NewEmail();

        var auth = await MagicLinkLoginAsync(email.ToUpperInvariant());

        Assert.Equal(email, auth.User.Email);
        Assert.Equal(["email"], auth.User.Providers);
        using var client = WithToken(auth.AccessToken);
        var me = await ReadAsync<UserResponse>(await client.GetAsync("/me"));
        Assert.Equal(auth.User.Id, me.Id);
    }

    [Fact]
    public async Task Magic_link_token_works_only_once()
    {
        var email = NewEmail();
        using var client = Anonymous();
        await client.PostAsJsonAsync("/auth/magic-link", new { email });
        var token = TokenFromLastEmail(email);

        var first = await client.PostAsJsonAsync("/auth/magic-link/verify", new { token });
        var second = await client.PostAsJsonAsync("/auth/magic-link/verify", new { token });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }

    [Fact]
    public async Task Same_email_signs_in_to_the_same_user()
    {
        var email = NewEmail();

        var first = await MagicLinkLoginAsync(email);
        var second = await MagicLinkLoginAsync(email);

        Assert.Equal(first.User.Id, second.User.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Magic_link_rejects_invalid_email(string email)
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/auth/magic-link", new { email });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_magic_link_token_is_rejected()
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/auth/magic-link/verify", new { token = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoints_require_a_token()
    {
        using var client = Anonymous();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/categories")).StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_reuse_revokes_the_session()
    {
        var auth = await MagicLinkLoginAsync(NewEmail());
        using var client = Anonymous();

        var refreshed = await ReadAsync<AuthResponse>(
            await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken }));
        Assert.NotEqual(auth.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(auth.User.Id, refreshed.User.Id);

        // Reusar o token antigo indica vazamento: derruba também o token novo.
        var reused = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        var afterReuse = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = refreshed.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var auth = await MagicLinkLoginAsync(NewEmail());
        using var client = Anonymous();

        var logout = await client.PostAsJsonAsync("/auth/logout", new { refreshToken = auth.RefreshToken });
        var refresh = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Apple_login_links_to_the_account_with_the_same_verified_email()
    {
        var email = NewEmail();
        var byEmail = await MagicLinkLoginAsync(email);
        using var client = Anonymous();

        var apple = await ReadAsync<AuthResponse>(await client.PostAsJsonAsync(
            "/auth/apple",
            new { idToken = $"valid|apple-{Guid.NewGuid():N}|{email}", name = "Fabricio" }));

        Assert.Equal(byEmail.User.Id, apple.User.Id);
        Assert.Equal(["apple", "email"], apple.User.Providers);
        Assert.Equal("Fabricio", apple.User.DisplayName);
    }

    [Fact]
    public async Task Apple_login_rejects_an_invalid_id_token()
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/auth/apple", new { idToken = "forged" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unconfigured_provider_returns_service_unavailable()
    {
        using var client = Anonymous();

        var response = await client.PostAsJsonAsync("/auth/google", new { idToken = "valid|x|x@example.com" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
