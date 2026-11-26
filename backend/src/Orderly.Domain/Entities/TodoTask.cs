namespace Orderly.Domain.Entities;

/// <summary>Tarefa. Chamada de TodoTask para não conflitar com System.Threading.Tasks.Task.</summary>
public sealed class TodoTask
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Notes { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.P4;
    public Guid? ProjectId { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
