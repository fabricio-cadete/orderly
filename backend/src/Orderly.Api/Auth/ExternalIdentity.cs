using Orderly.Domain.Entities;

namespace Orderly.Api.Auth;

/// <summary>Quem o provedor diz que o usuário é, depois de validar o token dele.</summary>
public sealed record ExternalIdentity(
    AuthProvider Provider,
    string Subject,
    string? Email,
    bool EmailVerified,
    string? Name);

/// <summary>Valida o id_token emitido por um provedor (Apple, Google).</summary>
public interface IExternalIdTokenValidator
{
    AuthProvider Provider { get; }

    /// <summary>Falso quando faltam credenciais (Client IDs) na configuração.</summary>
    bool IsConfigured { get; }

    /// <summary>Retorna null quando o token é inválido, expirado ou de outra audiência.</summary>
    Task<ExternalIdentity?> ValidateAsync(string idToken, CancellationToken ct);
}
