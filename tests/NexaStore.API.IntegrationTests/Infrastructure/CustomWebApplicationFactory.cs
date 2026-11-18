
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NexaStore.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Distributed;

namespace NexaStore.API.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            "NexaStore-IntegrationTests-Secret-Key-2026-AtLeast-32Chars");

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer",
            "NexaStore");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience",
            "NexaStore");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<NexaStoreDbContext>();

            services.RemoveAll<DbContextOptions<NexaStoreDbContext>>();

            services.RemoveAll<DbContextOptions>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<NexaStoreDbContext>>();

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            services.AddDbContext<NexaStoreDbContext>(options =>
            {
                options.UseInMemoryDatabase("NexaStoreIntegrationTests");
            });
        });
    }
}
