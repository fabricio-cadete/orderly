using Microsoft.EntityFrameworkCore;
using Orderly.Domain.Entities;

namespace Orderly.Infrastructure.Persistence;

public sealed class OrderlyDbContext(
    DbContextOptions<OrderlyDbContext> options,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : DbContext(options)
{
    public DbSet<TodoTask> Tasks => Set<TodoTask>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Lido a cada consulta pelos filtros globais. Sem usuário, vale Guid.Empty e nada é retornado.
    /// </summary>
    private Guid CurrentUserId => currentUser.UserId ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderlyDbContext).Assembly);

        // Isolamento por usuário: toda consulta de dados do usuário filtra pelo dono.
        modelBuilder.Entity<TodoTask>().HasQueryFilter(t => t.UserId == CurrentUserId);
        modelBuilder.Entity<Project>().HasQueryFilter(p => p.UserId == CurrentUserId);
        modelBuilder.Entity<Category>().HasQueryFilter(c => c.UserId == CurrentUserId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditAndOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditAndOwnership()
    {
        var now = timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is IUserOwned owned)
            {
                if (owned.UserId == Guid.Empty)
                {
                    owned.UserId = currentUser.UserId
                        ?? throw new InvalidOperationException("Não há usuário autenticado para ser dono do registro.");
                }
                else if (currentUser.UserId is { } userId && owned.UserId != userId)
                {
                    throw new InvalidOperationException("Registro atribuído a outro usuário.");
                }
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            SetIfExists(entry, "UpdatedAt", now);
            if (entry.State == EntityState.Added)
            {
                SetIfExists(entry, "CreatedAt", now);
            }
        }
    }

    private static void SetIfExists(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        string property,
        DateTimeOffset value)
    {
        if (entry.Metadata.FindProperty(property) is not null)
        {
            entry.Property(property).CurrentValue = value;
        }
    }
}
