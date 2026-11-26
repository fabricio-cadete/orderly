namespace Orderly.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    /// <summary>E-mail normalizado (minúsculo). Pode faltar quando a Apple não envia o e-mail.</summary>
    public string? Email { get; set; }

    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<UserIdentity> Identities { get; set; } = [];
}
