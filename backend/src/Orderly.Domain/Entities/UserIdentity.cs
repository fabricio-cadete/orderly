namespace Orderly.Domain.Entities;

/// <summary>Uma forma de login ligada ao usuário (provedor + identificador no provedor).</summary>
public sealed class UserIdentity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AuthProvider Provider { get; set; }

    /// <summary>Identificador do usuário no provedor ("sub" do id_token, ou o e-mail no link mágico).</summary>
    public required string Subject { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
