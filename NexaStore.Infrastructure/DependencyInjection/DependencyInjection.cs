using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Infrastructure.Persistence;
using NexaStore.Infrastructure.Persistence.Repositories;

namespace NexaStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("NexaStoreConnection")
            ?? throw new InvalidOperationException(
                "Database connection string was not found."
            );

        services.AddDbContext<NexaStoreDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}