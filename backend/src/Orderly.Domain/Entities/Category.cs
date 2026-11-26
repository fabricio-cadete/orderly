namespace Orderly.Domain.Entities;

/// <summary>Categoria = etiqueta transversal (@etiqueta).</summary>
public sealed class Category : IUserOwned
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<TodoTask> Tasks { get; set; } = [];
}
