using Microsoft.EntityFrameworkCore;
using Orderly.Domain.Entities;

namespace Orderly.Infrastructure.Persistence;

public sealed class OrderlyDbContext(DbContextOptions<OrderlyDbContext> options) : DbContext(options)
{
    public DbSet<TodoTask> Tasks => Set<TodoTask>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Category> Categories => Set<Category>();
}
