using Microsoft.EntityFrameworkCore;
using Orderly.Domain.Entities;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Api.Endpoints;

public sealed record TaskRequest(
    string? Title,
    string? Notes,
    TaskPriority? Priority,
    Guid? ProjectId,
    DateTimeOffset? DueDate,
    bool? DueHasTime,
    IReadOnlyList<Guid>? CategoryIds);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Notes,
    TaskPriority Priority,
    Guid? ProjectId,
    DateTimeOffset? DueDate,
    bool DueHasTime,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<Guid> CategoryIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

internal static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks").WithTags("Tasks").RequireAuthorization();

        group.MapGet("/", List);
        group.MapGet("/today", Today);
        group.MapGet("/{id:guid}", async (Guid id, OrderlyDbContext db, CancellationToken ct) =>
            await Query(db).FirstOrDefaultAsync(t => t.Id == id, ct) is { } task
                ? Results.Ok(ToResponse(task))
                : Results.NotFound());
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapPost("/{id:guid}/complete", (Guid id, OrderlyDbContext db, TimeProvider time, CancellationToken ct) =>
            SetCompleted(id, time.GetUtcNow(), db, ct));
        group.MapPost("/{id:guid}/reopen", (Guid id, OrderlyDbContext db, CancellationToken ct) =>
            SetCompleted(id, null, db, ct));
        group.MapDelete("/{id:guid}", async (Guid id, OrderlyDbContext db, CancellationToken ct) =>
        {
            var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (task is null)
            {
                return Results.NotFound();
            }

            db.Tasks.Remove(task);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static async Task<IResult> List(
        Guid? projectId,
        Guid? categoryId,
        bool? completed,
        OrderlyDbContext db,
        CancellationToken ct)
    {
        var query = Query(db);
        if (projectId is { } pid)
        {
            query = query.Where(t => t.ProjectId == pid);
        }

        if (categoryId is { } cid)
        {
            query = query.Where(t => t.Categories.Any(c => c.Id == cid));
        }

        if (completed is { } done)
        {
            query = done ? query.Where(t => t.CompletedAt != null) : query.Where(t => t.CompletedAt == null);
        }

        var tasks = await Ordered(query).ToListAsync(ct);
        return Results.Ok(tasks.Select(ToResponse));
    }

    /// <summary>
    /// Hoje = tarefas abertas sem data, vencidas ou que vencem até o fim do dia no fuso do usuário.
    /// O fuso vem em ?timeZone= (IANA, ex.: America/Sao_Paulo); o padrão é UTC.
    /// </summary>
    private static async Task<IResult> Today(string? timeZone, OrderlyDbContext db, TimeProvider time, CancellationToken ct)
    {
        var zone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(timeZone) && !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out zone))
        {
            return new Validation().Add("timeZone", "Fuso horário desconhecido. Use um nome IANA, como America/Sao_Paulo.").Problem();
        }

        var endOfToday = EndOfLocalDay(time.GetUtcNow(), zone!);
        var tasks = await Ordered(Query(db)
                .Where(t => t.CompletedAt == null && (t.DueDate == null || t.DueDate < endOfToday)))
            .ToListAsync(ct);
        return Results.Ok(tasks.Select(ToResponse));
    }

    internal static DateTimeOffset EndOfLocalDay(DateTimeOffset nowUtc, TimeZoneInfo zone)
    {
        var tomorrow = TimeZoneInfo.ConvertTime(nowUtc, zone).Date.AddDays(1);
        return new DateTimeOffset(tomorrow, zone.GetUtcOffset(tomorrow)).ToUniversalTime();
    }

    private static async Task<IResult> Create(TaskRequest body, OrderlyDbContext db, CancellationToken ct)
    {
        var task = new TodoTask { Id = Guid.CreateVersion7(), Title = "" };
        if (await ApplyAsync(task, body, db, ct) is { IsValid: false } validation)
        {
            return validation.Problem();
        }

        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/tasks/{task.Id}", ToResponse(task));
    }

    private static async Task<IResult> Update(Guid id, TaskRequest body, OrderlyDbContext db, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Categories).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            return Results.NotFound();
        }

        if (await ApplyAsync(task, body, db, ct) is { IsValid: false } validation)
        {
            return validation.Problem();
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> SetCompleted(Guid id, DateTimeOffset? completedAt, OrderlyDbContext db, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Categories).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            return Results.NotFound();
        }

        if (completedAt is null || task.CompletedAt is null)
        {
            task.CompletedAt = completedAt;
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(ToResponse(task));
    }

    /// <summary>Valida e copia o corpo para a tarefa (PUT substitui todos os campos editáveis).</summary>
    private static async Task<Validation> ApplyAsync(TodoTask task, TaskRequest body, OrderlyDbContext db, CancellationToken ct)
    {
        var validation = new Validation()
            .Text("title", body.Title, 500)
            .Text("notes", body.Notes, 10_000, required: false);

        if (body.Priority is { } priority && !Enum.IsDefined(priority))
        {
            validation.Add("priority", "Use P1, P2, P3 ou P4.");
        }

        // Os filtros por usuário fazem projetos e categorias de outra pessoa parecerem inexistentes.
        if (body.ProjectId is { } projectId && !await db.Projects.AnyAsync(p => p.Id == projectId, ct))
        {
            validation.Add("projectId", "Projeto não encontrado.");
        }

        var categoryIds = (body.CategoryIds ?? []).Distinct().ToList();
        var categories = categoryIds.Count == 0
            ? []
            : await db.Categories.Where(c => categoryIds.Contains(c.Id)).ToListAsync(ct);
        if (categories.Count != categoryIds.Count)
        {
            validation.Add("categoryIds", "Categoria não encontrada.");
        }

        if (!validation.IsValid)
        {
            return validation;
        }

        task.Title = body.Title!.Trim();
        task.Notes = string.IsNullOrWhiteSpace(body.Notes) ? null : body.Notes.Trim();
        task.Priority = body.Priority ?? TaskPriority.P4;
        task.ProjectId = body.ProjectId;
        task.DueDate = body.DueDate?.ToUniversalTime();
        task.DueHasTime = body.DueDate is not null && (body.DueHasTime ?? true);
        task.Categories.Clear();
        task.Categories.AddRange(categories);
        return validation;
    }

    private static IQueryable<TodoTask> Query(OrderlyDbContext db) =>
        db.Tasks.AsNoTracking().Include(t => t.Categories);

    private static IQueryable<TodoTask> Ordered(IQueryable<TodoTask> query) =>
        query.OrderBy(t => t.Priority)
            .ThenBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.CreatedAt);

    private static TaskResponse ToResponse(TodoTask t) => new(
        t.Id,
        t.Title,
        t.Notes,
        t.Priority,
        t.ProjectId,
        t.DueDate,
        t.DueHasTime,
        t.CompletedAt,
        t.Categories.Select(c => c.Id).Order().ToList(),
        t.CreatedAt,
        t.UpdatedAt);
}
