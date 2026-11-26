using Microsoft.EntityFrameworkCore;
using Orderly.Domain.Entities;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Api.Endpoints;

public sealed record CategoryRequest(string? Name);
public sealed record CategoryResponse(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

internal static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories").WithTags("Categories").RequireAuthorization();

        group.MapGet("/", async (OrderlyDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Categories.AsNoTracking().OrderBy(c => c.Name).Select(c => ToResponse(c)).ToListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, OrderlyDbContext db, CancellationToken ct) =>
            await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct) is { } category
                ? Results.Ok(ToResponse(category))
                : Results.NotFound());

        group.MapPost("/", async (CategoryRequest body, OrderlyDbContext db, CancellationToken ct) =>
        {
            if (await ValidateAsync(body, null, db, ct) is { IsValid: false } validation)
            {
                return validation.Problem();
            }

            var category = new Category { Id = Guid.CreateVersion7(), Name = body.Name!.Trim() };
            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/categories/{category.Id}", ToResponse(category));
        });

        group.MapPut("/{id:guid}", async (Guid id, CategoryRequest body, OrderlyDbContext db, CancellationToken ct) =>
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category is null)
            {
                return Results.NotFound();
            }

            if (await ValidateAsync(body, id, db, ct) is { IsValid: false } validation)
            {
                return validation.Problem();
            }

            category.Name = body.Name!.Trim();
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(category));
        });

        // Apagar a categoria só a remove das tarefas.
        group.MapDelete("/{id:guid}", async (Guid id, OrderlyDbContext db, CancellationToken ct) =>
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category is null)
            {
                return Results.NotFound();
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static async Task<Validation> ValidateAsync(CategoryRequest body, Guid? id, OrderlyDbContext db, CancellationToken ct)
    {
        var validation = new Validation().Text("name", body.Name, 100);
        if (validation.IsValid)
        {
            var name = body.Name!.Trim();
            if (await db.Categories.AnyAsync(c => c.Name == name && c.Id != id, ct))
            {
                validation.Add("name", "Já existe uma categoria com esse nome.");
            }
        }

        return validation;
    }

    private static CategoryResponse ToResponse(Category c) => new(c.Id, c.Name, c.CreatedAt, c.UpdatedAt);
}
