
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

using NexaStore.Application.Features.Categories.CreateCategory;
using NexaStore.Application.Features.Categories.GetCategoryById;
using NexaStore.Application.Features.Categories.GetAllCategories;
using NexaStore.Application.Features.Categories.UpdateCategory;
using NexaStore.Application.Features.Categories.DeleteCategory;

using NexaStore.Application.Features.Products.CreateProduct;
using NexaStore.Application.Features.Products.GetProductById;
using NexaStore.Application.Features.Products.GetAllProducts;
using NexaStore.Application.Features.Products.UpdateProduct;
using NexaStore.Application.Features.Products.DeleteProduct;

using NexaStore.Application.Features.Authentication.Register;
using NexaStore.Application.Features.Authentication.Login;
using NexaStore.Application.Features.Carts.GetCart;
using NexaStore.Application.Features.Carts.AddCartItem;
using NexaStore.Application.Features.Carts.UpdateCartItem;
using NexaStore.Application.Features.Carts.RemoveCartItem;
using NexaStore.Application.Features.Carts.ClearCart;
using NexaStore.Application.Features.Orders.CreateOrder;
using NexaStore.Application.Features.Orders.GetOrderById;
using NexaStore.Application.Features.Orders.GetMyOrders;
using NexaStore.Application.Features.Orders.CancelOrder;

namespace NexaStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // Categories
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<GetCategoryByIdHandler>();
        services.AddScoped<GetAllCategoriesHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<DeleteCategoryHandler>();

        // Products
        services.AddScoped<CreateProductHandler>();
        services.AddScoped<GetProductByIdHandler>();
        services.AddScoped<GetAllProductsHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<DeleteProductHandler>();

        // Authentication
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();

        services.AddScoped<GetCartHandler>();
        services.AddScoped<AddCartItemHandler>();
        services.AddScoped<UpdateCartItemHandler>();
        services.AddScoped<RemoveCartItemHandler>();
        services.AddScoped<ClearCartHandler>();
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<GetOrderByIdHandler>();
        services.AddScoped<GetMyOrdersHandler>();
        services.AddScoped<CancelOrderHandler>();

        // Validation
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);

        return services;
    }
}
