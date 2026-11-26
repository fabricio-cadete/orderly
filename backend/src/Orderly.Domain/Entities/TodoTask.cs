namespace Orderly.Domain.Entities;

/// <summary>Tarefa. Chamada de TodoTask para não conflitar com System.Threading.Tasks.Task.</summary>
public sealed class TodoTask : IUserOwned
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public string? Notes { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.P4;
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>Vencimento em UTC. Sem data, a tarefa aparece em Hoje.</summary>
    public DateTimeOffset? DueDate { get; set; }

    /// <summary>Falso quando o vencimento é só um dia, sem horário.</summary>
    public bool DueHasTime { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<Category> Categories { get; set; } = [];
}
