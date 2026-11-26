using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Orderly.Domain.Entities;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Api.Auth;

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>Emite o JWT de acesso e cuida do ciclo de vida dos refresh tokens.</summary>
internal sealed class TokenService(
    OrderlyDbContext db,
    IOptions<JwtOptions> options,
    TimeProvider time)
{
    private readonly JwtOptions _options = options.Value;

    public static SymmetricSecurityKey SigningKey(JwtOptions options) =>
        new(Encoding.UTF8.GetBytes(options.SigningKey));

    public async Task<AuthTokens> IssueAsync(User user, CancellationToken ct)
    {
        var refresh = NewRefreshToken(user.Id, out var rawRefresh);
        db.RefreshTokens.Add(refresh);
        await db.SaveChangesAsync(ct);
        return BuildTokens(user, rawRefresh, refresh.ExpiresAt);
    }

    /// <summary>
    /// Troca um refresh token válido por um novo par. Reusar um token já trocado indica vazamento:
    /// nesse caso todas as sessões do usuário são revogadas.
    /// </summary>
    public async Task<(AuthTokens Tokens, User User)?> RefreshAsync(string rawToken, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var hash = SecureToken.Hash(rawToken);
        var current = await db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (current is null)
        {
            return null;
        }

        if (current.RevokedAt is not null)
        {
            if (current.ReplacedById is not null)
            {
                await RevokeAllAsync(current.UserId, ct);
            }

            return null;
        }

        if (current.ExpiresAt <= now)
        {
            return null;
        }

        // Revoga de forma atômica: se duas requisições usarem o mesmo token, só uma vence.
        var revoked = await db.RefreshTokens
            .Where(t => t.Id == current.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
        if (revoked == 0)
        {
            await RevokeAllAsync(current.UserId, ct);
            return null;
        }

        var user = await db.Users.Include(u => u.Identities).FirstOrDefaultAsync(u => u.Id == current.UserId, ct);
        if (user is null)
        {
            return null;
        }

        var next = NewRefreshToken(user.Id, out var rawNext);
        db.RefreshTokens.Add(next);
        await db.SaveChangesAsync(ct);

        await db.RefreshTokens
            .Where(t => t.Id == current.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ReplacedById, next.Id), ct);

        return (BuildTokens(user, rawNext, next.ExpiresAt), user);
    }

    public async Task RevokeAsync(string rawToken, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var hash = SecureToken.Hash(rawToken);
        await db.RefreshTokens
            .Where(t => t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private async Task RevokeAllAsync(Guid userId, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private RefreshToken NewRefreshToken(Guid userId, out string raw)
    {
        raw = SecureToken.Create();
        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = SecureToken.Hash(raw),
            ExpiresAt = time.GetUtcNow().AddDays(_options.RefreshTokenDays),
        };
    }

    private AuthTokens BuildTokens(User user, string rawRefresh, DateTimeOffset refreshExpiresAt)
    {
        var now = time.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        if (user.Email is not null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(_options), SecurityAlgorithms.HmacSha256),
        };

        var accessToken = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AuthTokens(accessToken, expires, rawRefresh, refreshExpiresAt);
    }
}
