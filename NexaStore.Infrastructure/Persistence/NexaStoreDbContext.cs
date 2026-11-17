
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

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Cart>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedNever();

            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Items)
                .WithOne(x => x.Cart)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedNever();

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.HasIndex(x => new { x.CartId, x.ProductId })
                .IsUnique();

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
