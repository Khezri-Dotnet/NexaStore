using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexaStore.Application.Abstractions.Identity;
using NexaStore.Application.Abstractions.Persistence;
using NexaStore.Infrastructure.Identity;
using NexaStore.Infrastructure.Persistence;
using NexaStore.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using NexaStore.Application.Abstractions.Authentication;

namespace NexaStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string was not found."
            );

        services.AddDbContext<NexaStoreDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        services
        .AddIdentityCore<ApplicationUser>()
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<NexaStoreDbContext>();
        // .AddDefaultTokenProviders();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<Application.Abstractions.Authentication.IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        

        return services;
    }
}