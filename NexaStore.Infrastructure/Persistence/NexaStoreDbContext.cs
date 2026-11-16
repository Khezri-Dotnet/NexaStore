
using Microsoft.EntityFrameworkCore;
using NexaStore.Domain.Entities;

namespace NexaStore.Infrastructure.Persistence;

public sealed class NexaStoreDbContext : DbContext
{
    public NexaStoreDbContext(
        DbContextOptions<NexaStoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(NexaStoreDbContext).Assembly
        );

        modelBuilder.Entity<Product>()
            .HasQueryFilter(x => !x.IsDeleted);
    }
}
