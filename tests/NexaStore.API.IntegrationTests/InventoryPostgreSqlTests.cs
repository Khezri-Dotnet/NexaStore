
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaStore.Application.Features.Orders.CreateOrder;
using NexaStore.Domain.Entities;
using NexaStore.Domain.Enums;
using NexaStore.Infrastructure.Identity;
using NexaStore.Infrastructure.Persistence;
using Xunit;
using NexaStore.Application.Features.Orders.CancelOrder;

namespace NexaStore.API.IntegrationTests;

public sealed class InventoryPostgreSqlTests
{
    [Fact]
    public async Task CreateOrder_ShouldReserveStockAndCreateMovement()
    {
        using var factory = new Infrastructure.PostgreSqlWebApplicationFactory();

        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();

        var services = scope.ServiceProvider;

        var context =
            services.GetRequiredService<NexaStoreDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"inventory-{Guid.NewGuid():N}@test.com";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var userResult = await userManager.CreateAsync(
            user,
            "Test@Password12345!");

        Assert.True(
            userResult.Succeeded,
            string.Join(", ", userResult.Errors.Select(x => x.Description)));

        var roleResult = await userManager.AddToRoleAsync(
            user,
            "Customer");

        Assert.True(roleResult.Succeeded);

        var category = new Category(
            $"Inventory-{Guid.NewGuid():N}",
            "Inventory integration test");

        await context.Categories.AddAsync(category);

        var product = new Product(
            "Inventory Test Product",
            "PostgreSQL inventory test",
            100,
            10,
            category.Id);

        await context.Products.AddAsync(product);

        var cart = new Cart(user.Id);
        cart.AddItem(product, 2);

        await context.Carts.AddAsync(cart);

        await context.SaveChangesAsync();

        var handler =
            services.GetRequiredService<CreateOrderHandler>();

        var result = await handler.Handle(user.Id);

        Assert.True(result.IsSuccess);

        var persistedProduct = await context.Products
            .AsNoTracking()
            .SingleAsync(x => x.Id == product.Id);

        Assert.Equal(8, persistedProduct.StockQuantity);

        var reservation = await context.StockReservations
            .AsNoTracking()
            .SingleAsync(x => x.OrderId == result.Value);

        Assert.Equal(product.Id, reservation.ProductId);
        Assert.Equal(2, reservation.Quantity);
        Assert.Equal(StockReservationStatus.Active, reservation.Status);

        var movement = await context.StockMovements
            .AsNoTracking()
            .SingleAsync(x => x.ReservationId == reservation.Id);

        Assert.Equal(StockMovementType.Reserved, movement.Type);
        Assert.Equal(10, movement.StockBefore);
        Assert.Equal(8, movement.StockAfter);
        Assert.Equal(2, movement.Quantity);
    }


    [Fact]
    public async Task CancelOrder_ShouldReleaseReservationAndRestoreStock()
    {
        using var factory = new Infrastructure.PostgreSqlWebApplicationFactory();

        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();

        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<NexaStoreDbContext>();

        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"cancel-{Guid.NewGuid():N}@test.com";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var userResult = await userManager.CreateAsync(
            user,
            "Test@Password12345!");

        Assert.True(userResult.Succeeded);

        var roleResult = await userManager.AddToRoleAsync(user, "Customer");
        Assert.True(roleResult.Succeeded);

        var category = new Category(
            $"Cancel-{Guid.NewGuid():N}",
            "Cancellation test");

        var product = new Product(
            "Cancel Test Product",
            "PostgreSQL cancellation test",
            100,
            10,
            category.Id);

        var cart = new Cart(user.Id);
        cart.AddItem(product, 3);

        await context.Categories.AddAsync(category);
        await context.Products.AddAsync(product);
        await context.Carts.AddAsync(cart);
        await context.SaveChangesAsync();

        var createHandler =
            services.GetRequiredService<CreateOrderHandler>();

        var createResult = await createHandler.Handle(user.Id);

        Assert.True(createResult.IsSuccess);

        var cancelHandler =
            services.GetRequiredService<CancelOrderHandler>();

        var cancelResult = await cancelHandler.Handle(
            createResult.Value,
            user.Id);

        Assert.True(cancelResult.IsSuccess);

        var persistedProduct = await context.Products
            .AsNoTracking()
            .SingleAsync(x => x.Id == product.Id);

        Assert.Equal(10, persistedProduct.StockQuantity);

        var reservation = await context.StockReservations
            .AsNoTracking()
            .SingleAsync(x => x.OrderId == createResult.Value);

        Assert.Equal(StockReservationStatus.Released, reservation.Status);

        var movements = await context.StockMovements
            .AsNoTracking()
            .Where(x => x.OrderId == createResult.Value)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, movements.Count);

        Assert.Equal(StockMovementType.Reserved, movements[0].Type);
        Assert.Equal(StockMovementType.ReservationReleased, movements[1].Type);

        Assert.Equal(3, movements[1].Quantity);
        Assert.Equal(7, movements[1].StockBefore);
        Assert.Equal(10, movements[1].StockAfter);
    }


    [Fact]
    public async Task ConcurrentCancel_ShouldRestoreStockOnlyOnce()
    {
        using var factory = new Infrastructure.PostgreSqlWebApplicationFactory();

        await factory.InitializeDatabaseAsync();

        await using var scope = factory.Services.CreateAsyncScope();

        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<NexaStoreDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = $"concurrent-cancel-{Guid.NewGuid():N}@test.com";
        var user = new ApplicationUser { UserName = email, Email = email };

        var userResult = await userManager.CreateAsync(user, "Test@Password12345!");
        Assert.True(userResult.Succeeded);

        var roleResult = await userManager.AddToRoleAsync(user, "Customer");
        Assert.True(roleResult.Succeeded);

        var category = new Category($"Concurrent-{Guid.NewGuid():N}", "Concurrency test");
        var product = new Product("Concurrent Product", "Test", 100, 10, category.Id);
        var cart = new Cart(user.Id);
        cart.AddItem(product, 3);

        await context.Categories.AddAsync(category);
        await context.Products.AddAsync(product);
        await context.Carts.AddAsync(cart);
        await context.SaveChangesAsync();

        var createHandler = services.GetRequiredService<CreateOrderHandler>();
        var createResult = await createHandler.Handle(user.Id);

        Assert.True(createResult.IsSuccess);

        var orderId = createResult.Value;

        await using var scope1 = factory.Services.CreateAsyncScope();
        await using var scope2 = factory.Services.CreateAsyncScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<CancelOrderHandler>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<CancelOrderHandler>();

        var results = await Task.WhenAll(
            handler1.Handle(orderId, user.Id),
            handler2.Handle(orderId, user.Id));

        Assert.Equal(1, results.Count(x => x.IsSuccess));

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<NexaStoreDbContext>();

        var persistedProduct = await verifyContext.Products
            .AsNoTracking()
            .SingleAsync(x => x.Id == product.Id);

        Assert.Equal(10, persistedProduct.StockQuantity);

        var movements = await verifyContext.StockMovements
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .ToListAsync();

        Assert.Equal(2, movements.Count);
        Assert.Single(movements.Where(
            x => x.Type == StockMovementType.ReservationReleased));
    }
}
