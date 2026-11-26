namespace Orderly.Domain.Entities;

/// <summary>Projeto = lista com cor.</summary>
public sealed class Project : IUserOwned
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }

    /// <summary>Cor em hexadecimal, no formato #RRGGBB.</summary>
    public required string Color { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
