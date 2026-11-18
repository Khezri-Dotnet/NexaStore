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
using NexaStore.Infrastructure.Services;
using NexaStore.Application.Common.Interfaces;

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

        // services
        // .AddIdentityCore<ApplicationUser>()
        // .AddRoles<IdentityRole<Guid>>()
        // .AddEntityFrameworkStores<NexaStoreDbContext>();
       

        // این تغییر سه قابلیت امنیتی ایجاد می‌کند:
        // - حداقل رمز عبور ۱۲ کاراکتر با ترکیب الزامی حروف، عدد و نماد.
        // - جلوگیری از ثبت ایمیل تکراری.
        // - قفل موقت حساب پس از ۵ تلاش ناموفق
        services
        .AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.User.RequireUniqueEmail = true;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<NexaStoreDbContext>();
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "NexaStore:";
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<Application.Abstractions.Authentication.IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ICacheService, RedisCacheService>();
        

        return services;
    }
}