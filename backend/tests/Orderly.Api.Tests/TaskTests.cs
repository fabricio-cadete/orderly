using System.Net;
using System.Net.Http.Json;
using Orderly.Api.Endpoints;
using Orderly.Api.Tests.Infrastructure;
using Orderly.Domain.Entities;

namespace Orderly.Api.Tests;

public sealed class TaskTests(OrderlyApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Task_crud_with_project_categories_and_completion()
    {
        using var client = await SignedInClientAsync();
        var project = await ReadAsync<ProjectResponse>(
            await client.PostAsJsonAsync("/projects", new { name = "Casa", color = "#ff0000" }));
        Assert.Equal("#FF0000", project.Color);
        var category = await ReadAsync<CategoryResponse>(
            await client.PostAsJsonAsync("/categories", new { name = "mercado" }));
        var due = DateTimeOffset.Parse("2026-12-01T18:30:00-03:00");

        var created = await client.PostAsJsonAsync("/tasks", new
        {
            title = "  Comprar café  ",
            priority = "P1",
            projectId = project.Id,
            dueDate = due,
            categoryIds = new[] { category.Id },
        }, Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var task = await ReadAsync<TaskResponse>(created);
        Assert.Equal("Comprar café", task.Title);
        Assert.Equal(TaskPriority.P1, task.Priority);
        Assert.Equal(project.Id, task.ProjectId);
        Assert.Equal(due, task.DueDate);
        Assert.True(task.DueHasTime);
        Assert.Equal([category.Id], task.CategoryIds);

        var updated = await ReadAsync<TaskResponse>(await client.PutAsJsonAsync($"/tasks/{task.Id}", new
        {
            title = "Comprar café e pão",
            priority = "P2",
        }, Json));
        Assert.Equal("Comprar café e pão", updated.Title);
        Assert.Equal(TaskPriority.P2, updated.Priority);
        Assert.Null(updated.ProjectId);
        Assert.Null(updated.DueDate);
        Assert.Empty(updated.CategoryIds);

        var completed = await ReadAsync<TaskResponse>(await client.PostAsync($"/tasks/{task.Id}/complete", null));
        Assert.NotNull(completed.CompletedAt);
        var open = await ReadAsync<List<TaskResponse>>(await client.GetAsync("/tasks?completed=false"));
        Assert.DoesNotContain(open, t => t.Id == task.Id);

        var reopened = await ReadAsync<TaskResponse>(await client.PostAsync($"/tasks/{task.Id}/reopen", null));
        Assert.Null(reopened.CompletedAt);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/tasks/{task.Id}")).StatusCode);
    }

    [Fact]
    public async Task New_task_defaults_to_p4_without_date()
    {
        using var client = await SignedInClientAsync();

        var task = await ReadAsync<TaskResponse>(await client.PostAsJsonAsync("/tasks", new { title = "Ligar" }));

        Assert.Equal(TaskPriority.P4, task.Priority);
        Assert.Null(task.DueDate);
        Assert.False(task.DueHasTime);
    }

    [Fact]
    public async Task Filters_by_project_and_category()
    {
        using var client = await SignedInClientAsync();
        var project = await ReadAsync<ProjectResponse>(
            await client.PostAsJsonAsync("/projects", new { name = "Trabalho", color = "#00AA00" }));
        var category = await ReadAsync<CategoryResponse>(
            await client.PostAsJsonAsync("/categories", new { name = "urgente" }));
        var inProject = await ReadAsync<TaskResponse>(
            await client.PostAsJsonAsync("/tasks", new { title = "No projeto", projectId = project.Id }));
        var withCategory = await ReadAsync<TaskResponse>(
            await client.PostAsJsonAsync("/tasks", new { title = "Com categoria", categoryIds = new[] { category.Id } }));

        var byProject = await ReadAsync<List<TaskResponse>>(await client.GetAsync($"/tasks?projectId={project.Id}"));
        var byCategory = await ReadAsync<List<TaskResponse>>(await client.GetAsync($"/tasks?categoryId={category.Id}"));

        Assert.Equal([inProject.Id], byProject.Select(t => t.Id));
        Assert.Equal([withCategory.Id], byCategory.Select(t => t.Id));
    }

    [Fact]
    public async Task Validation_errors_return_bad_request()
    {
        using var client = await SignedInClientAsync();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/tasks", new { title = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/tasks", new { title = "x", priority = "P9" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/tasks", new { title = "x", projectId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/projects", new { name = "x", color = "red" })).StatusCode);
        await client.PostAsJsonAsync("/categories", new { name = "repetida" });
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/categories", new { name = "repetida" })).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_project_keeps_its_tasks_without_project()
    {
        using var client = await SignedInClientAsync();
        var project = await ReadAsync<ProjectResponse>(
            await client.PostAsJsonAsync("/projects", new { name = "Temporário", color = "#123456" }));
        var task = await ReadAsync<TaskResponse>(
            await client.PostAsJsonAsync("/tasks", new { title = "Sobrevive", projectId = project.Id }));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/projects/{project.Id}")).StatusCode);

        var after = await ReadAsync<TaskResponse>(await client.GetAsync($"/tasks/{task.Id}"));
        Assert.Null(after.ProjectId);
    }

    [Fact]
    public async Task Today_lists_open_tasks_without_date_overdue_or_due_today()
    {
        using var client = await SignedInClientAsync();
        var now = DateTimeOffset.UtcNow;
        async Task<Guid> Create(object body) =>
            (await ReadAsync<TaskResponse>(await client.PostAsJsonAsync("/tasks", body))).Id;

        var noDate = await Create(new { title = "Sem data" });
        var overdue = await Create(new { title = "Atrasada", dueDate = now.AddDays(-2) });
        var future = await Create(new { title = "Semana que vem", dueDate = now.AddDays(7) });
        var done = await Create(new { title = "Feita" });
        await client.PostAsync($"/tasks/{done}/complete", null);

        var today = await ReadAsync<List<TaskResponse>>(await client.GetAsync("/tasks/today?timeZone=America/Sao_Paulo"));
        var ids = today.Select(t => t.Id).ToList();

        Assert.Contains(noDate, ids);
        Assert.Contains(overdue, ids);
        Assert.DoesNotContain(future, ids);
        Assert.DoesNotContain(done, ids);
    }

    [Fact]
    public async Task Today_rejects_unknown_time_zone()
    {
        using var client = await SignedInClientAsync();

        var response = await client.GetAsync("/tasks/today?timeZone=Mars/Olympus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void End_of_local_day_uses_the_users_time_zone()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        // 23h de 26/11 em São Paulo já é 27/11 em UTC; o dia dele só acaba às 03h UTC de 27/11.
        var end = TaskEndpoints.EndOfLocalDay(DateTimeOffset.Parse("2026-11-27T02:00:00Z"), zone);

        Assert.Equal(DateTimeOffset.Parse("2026-11-27T03:00:00Z"), end);
    }
}
