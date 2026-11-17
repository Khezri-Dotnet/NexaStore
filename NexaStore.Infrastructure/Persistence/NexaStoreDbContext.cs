
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NexaStore.Domain.Entities;
using NexaStore.Infrastructure.Identity;

namespace NexaStore.Infrastructure.Persistence;

public sealed class NexaStoreDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public NexaStoreDbContext(
        DbContextOptions<NexaStoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();
}
