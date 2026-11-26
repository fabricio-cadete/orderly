namespace Orderly.Infrastructure.Persistence;

/// <summary>Usuário autenticado na requisição atual. Null quando não há login.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
}

/// <summary>Usado fora de uma requisição (migrações, ferramentas): não enxerga dados de usuário.</summary>
public sealed class NoCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
}
