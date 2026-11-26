namespace Orderly.Domain.Entities;

/// <summary>Projeto = lista com cor.</summary>
public sealed class Project
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
