using System.Net;
using System.Net.Http.Json;
using Orderly.Api.Endpoints;
using Orderly.Api.Tests.Infrastructure;

namespace Orderly.Api.Tests;

/// <summary>Cada usuário só vê, altera e usa os próprios dados.</summary>
public sealed class IsolationTests(OrderlyApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Users_cannot_see_or_change_each_others_data()
    {
        using var alice = await SignedInClientAsync();
        using var bob = await SignedInClientAsync();

        var project = await ReadAsync<ProjectResponse>(
            await alice.PostAsJsonAsync("/projects", new { name = "Privado", color = "#AA00AA" }));
        var category = await ReadAsync<CategoryResponse>(
            await alice.PostAsJsonAsync("/categories", new { name = "segredo" }));
        var task = await ReadAsync<TaskResponse>(await alice.PostAsJsonAsync("/tasks", new
        {
            title = "Só da Alice",
            projectId = project.Id,
            categoryIds = new[] { category.Id },
        }));

        Assert.Empty(await ReadAsync<List<TaskResponse>>(await bob.GetAsync("/tasks")));
        Assert.Empty(await ReadAsync<List<TaskResponse>>(await bob.GetAsync("/tasks/today")));
        Assert.Empty(await ReadAsync<List<ProjectResponse>>(await bob.GetAsync("/projects")));
        Assert.Empty(await ReadAsync<List<CategoryResponse>>(await bob.GetAsync("/categories")));

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await bob.PutAsJsonAsync($"/tasks/{task.Id}", new { title = "invadido" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsync($"/tasks/{task.Id}/complete", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/projects/{project.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await bob.PutAsJsonAsync($"/categories/{category.Id}", new { name = "x" })).StatusCode);

        // Também não dá para pendurar uma tarefa no projeto ou na categoria de outra pessoa.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await bob.PostAsJsonAsync("/tasks", new { title = "x", projectId = project.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await bob.PostAsJsonAsync("/tasks", new { title = "x", categoryIds = new[] { category.Id } })).StatusCode);

        // Bob pode usar o mesmo nome de categoria que a Alice.
        Assert.Equal(HttpStatusCode.Created,
            (await bob.PostAsJsonAsync("/categories", new { name = "segredo" })).StatusCode);

        var stillThere = await ReadAsync<TaskResponse>(await alice.GetAsync($"/tasks/{task.Id}"));
        Assert.Equal("Só da Alice", stillThere.Title);
    }
}
