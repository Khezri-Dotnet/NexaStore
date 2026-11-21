using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NexaStore.Infrastructure.Persistence;

namespace NexaStore.API.IntegrationTests.Infrastructure;

public sealed class PostgreSqlWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string ConnectionString =
        "Host=localhost;Port=5435;Database=NexaStoreTestDb;Username=postgres;Password=postgres";

    public PostgreSqlWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            "NexaStore.IntegrationTests.SigningKey.2026!VeryLongSecret");

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer",
            "NexaStore.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience",
            "NexaStore.Tests");

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            ConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                    ["Jwt:Key"] = "NexaStore.IntegrationTests.SigningKey.2026!VeryLongSecret",
                    ["Jwt:Issuer"] = "NexaStore.Tests",
                    ["Jwt:Audience"] = "NexaStore.Tests"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<NexaStoreDbContext>();
            services.RemoveAll<DbContextOptions<NexaStoreDbContext>>();
            services.RemoveAll<DbContextOptions>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<NexaStoreDbContext>>();

            services.AddDbContext<NexaStoreDbContext>(options =>
                options.UseNpgsql(ConnectionString));

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();

        var context = scope.ServiceProvider
            .GetRequiredService<NexaStoreDbContext>();

        await context.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in new[] { "Admin", "Customer" })
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(
                new IdentityRole<Guid>(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role {roleName}: " +
                    string.Join(", ", result.Errors.Select(x => x.Description)));
            }
        }
    }
}