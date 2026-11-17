
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

        // Validation
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);

        return services;
    }
}
