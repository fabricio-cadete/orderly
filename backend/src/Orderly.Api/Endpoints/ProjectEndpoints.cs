using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Orderly.Domain.Entities;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Api.Endpoints;

public sealed record ProjectRequest(string? Name, string? Color);
public sealed record ProjectResponse(Guid Id, string Name, string Color, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

internal static partial class ProjectEndpoints
{
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();

    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/projects").WithTags("Projects").RequireAuthorization();

        group.MapGet("/", async (OrderlyDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Projects.AsNoTracking().OrderBy(p => p.Name).Select(p => ToResponse(p)).ToListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, OrderlyDbContext db, CancellationToken ct) =>
            await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct) is { } project
                ? Results.Ok(ToResponse(project))
                : Results.NotFound());

        group.MapPost("/", async (ProjectRequest body, OrderlyDbContext db, CancellationToken ct) =>
        {
            if (Validate(body) is { IsValid: false } validation)
            {
                return validation.Problem();
            }

            var project = new Project { Id = Guid.CreateVersion7(), Name = body.Name!.Trim(), Color = body.Color!.ToUpperInvariant() };
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/projects/{project.Id}", ToResponse(project));
        });

        group.MapPut("/{id:guid}", async (Guid id, ProjectRequest body, OrderlyDbContext db, CancellationToken ct) =>
        {
            if (Validate(body) is { IsValid: false } validation)
            {
                return validation.Problem();
            }

            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
            {
                return Results.NotFound();
            }

            project.Name = body.Name!.Trim();
            project.Color = body.Color!.ToUpperInvariant();
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(project));
        });

        // As tarefas do projeto não são apagadas: ficam sem projeto.
        group.MapDelete("/{id:guid}", async (Guid id, OrderlyDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (project is null)
            {
                return Results.NotFound();
            }

            db.Projects.Remove(project);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static Validation Validate(ProjectRequest body)
    {
        var validation = new Validation().Text("name", body.Name, 200);
        if (body.Color is null || !HexColor().IsMatch(body.Color))
        {
            validation.Add("color", "Use o formato #RRGGBB.");
        }

        return validation;
    }

    private static ProjectResponse ToResponse(Project p) => new(p.Id, p.Name, p.Color, p.CreatedAt, p.UpdatedAt);
}
