using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orderly.Infrastructure.Persistence;

namespace Orderly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderlyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Orderly")
            ?? throw new InvalidOperationException("ConnectionStrings:Orderly não configurada.");

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, NoCurrentUser>();
        services.AddDbContext<OrderlyDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
