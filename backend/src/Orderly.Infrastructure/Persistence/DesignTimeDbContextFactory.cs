using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orderly.Infrastructure.Persistence;

/// <summary>Usado pelo `dotnet ef` para gerar migrações sem subir a API.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderlyDbContext>
{
    public OrderlyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Orderly")
            ?? "Host=localhost;Port=5432;Database=orderly;Username=orderly;Password=orderly";

        var options = new DbContextOptionsBuilder<OrderlyDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OrderlyDbContext(options, new NoCurrentUser(), TimeProvider.System);
    }
}
