using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orderly.Api.Auth;
using Orderly.Api.Email;
using Orderly.Domain.Entities;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Api.Endpoints;

public sealed record MagicLinkRequest(string? Email);
public sealed record MagicLinkVerifyRequest(string? Token);
public sealed record IdTokenRequest(string? IdToken, string? Name);
public sealed record RefreshRequest(string? RefreshToken);
public sealed record UserResponse(Guid Id, string? Email, string? DisplayName, IReadOnlyList<string> Providers);
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserResponse User);

internal static class AuthEndpoints
{
    public const string RateLimitPolicy = "auth";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth").RequireRateLimiting(RateLimitPolicy);

        auth.MapPost("/magic-link", RequestMagicLink);
        auth.MapPost("/magic-link/verify", VerifyMagicLink);
        auth.MapPost("/apple", (IdTokenRequest body, AuthDeps deps, CancellationToken ct) =>
            SignInWithIdToken(AuthProvider.Apple, body, deps, ct));
        auth.MapPost("/google", (IdTokenRequest body, AuthDeps deps, CancellationToken ct) =>
            SignInWithIdToken(AuthProvider.Google, body, deps, ct));
        auth.MapPost("/refresh", Refresh);
        auth.MapPost("/logout", Logout);

        app.MapGet("/me", Me).WithTags("Auth").RequireAuthorization();
    }

    /// <summary>Sempre responde 202, exista ou não a conta, para não revelar quem está cadastrado.</summary>
    private static async Task<IResult> RequestMagicLink(
        MagicLinkRequest body,
        OrderlyDbContext db,
        IEmailSender email,
        IOptions<MagicLinkOptions> options,
        TimeProvider time,
        CancellationToken ct)
    {
        var address = AccountService.NormalizeEmail(body.Email);
        if (address is null)
        {
            return new Validation().Add("email", "E-mail inválido.").Problem();
        }

        var token = SecureToken.Create();
        db.MagicLinkTokens.Add(new MagicLinkToken
        {
            Id = Guid.CreateVersion7(),
            Email = address,
            TokenHash = SecureToken.Hash(token),
            ExpiresAt = time.GetUtcNow().AddMinutes(options.Value.ExpiresMinutes),
        });
        await db.SaveChangesAsync(ct);

        var separator = options.Value.LinkBaseUrl.Contains('?') ? '&' : '?';
        var link = $"{options.Value.LinkBaseUrl}{separator}token={Uri.EscapeDataString(token)}";
        var minutes = options.Value.ExpiresMinutes;
        await email.SendAsync(new EmailMessage(
            address,
            "Seu link de acesso ao Orderly",
            $"Abra este link para entrar no Orderly: {link}\n\nEle vale por {minutes} minutos e só pode ser usado uma vez.",
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Entrar no Orderly</a></p>" +
            $"<p>O link vale por {minutes} minutos e só pode ser usado uma vez.</p>"),
            ct);

        return Results.Accepted();
    }

    private static async Task<IResult> VerifyMagicLink(
        MagicLinkVerifyRequest body,
        AuthDeps deps,
        TimeProvider time,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Token))
        {
            return Results.Unauthorized();
        }

        var now = time.GetUtcNow();
        var hash = SecureToken.Hash(body.Token);
        var token = await deps.Db.MagicLinkTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.ConsumedAt == null && t.ExpiresAt > now, ct);
        if (token is null)
        {
            return Results.Unauthorized();
        }

        // Consumo atômico: o mesmo link não pode abrir duas sessões.
        var consumed = await deps.Db.MagicLinkTokens
            .Where(t => t.Id == token.Id && t.ConsumedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now), ct);
        if (consumed == 0)
        {
            return Results.Unauthorized();
        }

        var identity = new ExternalIdentity(AuthProvider.Email, token.Email, token.Email, EmailVerified: true, Name: null);
        return await SignInAsync(identity, deps, ct);
    }

    private static async Task<IResult> SignInWithIdToken(
        AuthProvider provider,
        IdTokenRequest body,
        AuthDeps deps,
        CancellationToken ct)
    {
        var validator = deps.Validators.FirstOrDefault(v => v.Provider == provider);
        if (validator is null || !validator.IsConfigured)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: $"Login com {provider} ainda não está configurado no servidor.");
        }

        if (string.IsNullOrWhiteSpace(body.IdToken))
        {
            return new Validation().Add("idToken", "Obrigatório.").Problem();
        }

        var identity = await validator.ValidateAsync(body.IdToken, ct);
        if (identity is null)
        {
            return Results.Unauthorized();
        }

        // A Apple só manda o nome no primeiro login, fora do id_token; o app repassa em "name".
        if (identity.Name is null && !string.IsNullOrWhiteSpace(body.Name))
        {
            identity = identity with { Name = body.Name };
        }

        return await SignInAsync(identity, deps, ct);
    }

    private static async Task<IResult> Refresh(RefreshRequest body, AuthDeps deps, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            return Results.Unauthorized();
        }

        var result = await deps.Tokens.RefreshAsync(body.RefreshToken, ct);
        return result is { } ok ? Results.Ok(ToResponse(ok.Tokens, ok.User)) : Results.Unauthorized();
    }

    private static async Task<IResult> Logout(RefreshRequest body, TokenService tokens, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            await tokens.RevokeAsync(body.RefreshToken, ct);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> Me(ICurrentUser current, OrderlyDbContext db, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Identities).FirstOrDefaultAsync(u => u.Id == current.UserId, ct);
        return user is null ? Results.Unauthorized() : Results.Ok(ToUser(user));
    }

    private static async Task<IResult> SignInAsync(ExternalIdentity identity, AuthDeps deps, CancellationToken ct)
    {
        var user = await deps.Accounts.FindOrCreateAsync(identity, ct);
        var tokens = await deps.Tokens.IssueAsync(user, ct);
        await deps.Db.Entry(user).Collection(u => u.Identities).LoadAsync(ct);
        return Results.Ok(ToResponse(tokens, user));
    }

    private static AuthResponse ToResponse(AuthTokens tokens, User user) => new(
        tokens.AccessToken,
        tokens.AccessTokenExpiresAt,
        tokens.RefreshToken,
        tokens.RefreshTokenExpiresAt,
        ToUser(user));

    private static UserResponse ToUser(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Identities.Select(i => i.Provider.ToString().ToLowerInvariant()).Distinct().Order().ToList());
}

/// <summary>Agrupa as dependências comuns dos endpoints de login.</summary>
internal sealed record AuthDeps(
    OrderlyDbContext Db,
    AccountService Accounts,
    TokenService Tokens,
    IEnumerable<IExternalIdTokenValidator> Validators);
