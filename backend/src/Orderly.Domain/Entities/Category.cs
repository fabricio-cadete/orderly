namespace Orderly.Domain.Entities;

/// <summary>Categoria = etiqueta transversal (@etiqueta).</summary>
public sealed class Category
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
