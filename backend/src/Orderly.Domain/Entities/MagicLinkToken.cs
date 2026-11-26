namespace Orderly.Domain.Entities;

/// <summary>Token de uso único enviado por e-mail. Só o hash fica salvo.</summary>
public sealed class MagicLinkToken
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
