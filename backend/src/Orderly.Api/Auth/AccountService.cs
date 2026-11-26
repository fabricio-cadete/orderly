using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Orderly.Domain.Entities;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Api.Auth;

/// <summary>Encontra ou cria o usuário de uma identidade já validada.</summary>
internal sealed class AccountService(OrderlyDbContext db)
{
    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        return MailAddress.TryCreate(trimmed, out var address) && address.Address == trimmed
            ? trimmed.ToLowerInvariant()
            : null;
    }

    public async Task<User> FindOrCreateAsync(ExternalIdentity identity, CancellationToken ct)
    {
        var existing = await db.UserIdentities
            .Where(i => i.Provider == identity.Provider && i.Subject == identity.Subject)
            .Select(i => i.UserId)
            .FirstOrDefaultAsync(ct);
        if (existing != Guid.Empty)
        {
            var known = await db.Users.FirstAsync(u => u.Id == existing, ct);
            await FillDisplayNameAsync(known, identity.Name, ct);
            return known;
        }

        // Só liga contas pelo e-mail quando o provedor garante que o e-mail é do usuário.
        var email = identity.EmailVerified ? NormalizeEmail(identity.Email) : null;
        var user = email is null ? null : await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            user = new User { Id = Guid.CreateVersion7(), Email = email, DisplayName = Trim(identity.Name) };
            db.Users.Add(user);
        }
        else if (user.DisplayName is null)
        {
            user.DisplayName = Trim(identity.Name);
        }

        db.UserIdentities.Add(new UserIdentity
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Provider = identity.Provider,
            Subject = identity.Subject,
        });

        await db.SaveChangesAsync(ct);
        return user;
    }

    private async Task FillDisplayNameAsync(User user, string? name, CancellationToken ct)
    {
        if (user.DisplayName is null && Trim(name) is { } trimmed)
        {
            user.DisplayName = trimmed;
            await db.SaveChangesAsync(ct);
        }
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 200)];
}
