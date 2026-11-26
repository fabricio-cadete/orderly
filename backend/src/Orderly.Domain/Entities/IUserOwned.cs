namespace Orderly.Domain.Entities;

/// <summary>Entidade que pertence a um único usuário. Cada usuário só enxerga os próprios dados.</summary>
public interface IUserOwned
{
    Guid UserId { get; set; }
}
