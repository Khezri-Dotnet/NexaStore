
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
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(NexaStoreDbContext).Assembly);

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

        // Order configuration
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.OrderDate)
                .IsRequired();

            entity.Property(x => x.Status)
                .IsRequired();

            entity.Navigation(x => x.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.OwnsMany(x => x.Items, item =>
            {
                item.ToTable("OrderItems");

                item.WithOwner()
                    .HasForeignKey("OrderId");

                item.Property<Guid>("Id");

                item.HasKey("Id");

                item.Property(x => x.ProductId)
                    .IsRequired();

                item.Property(x => x.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                item.Property(x => x.UnitPrice)
                    .HasPrecision(18, 2);

                item.Property(x => x.Quantity)
                    .IsRequired();
            });
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(x => x.ReplacedByTokenHash)
                .HasMaxLength(64);

            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasIndex(x => x.UserId);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
