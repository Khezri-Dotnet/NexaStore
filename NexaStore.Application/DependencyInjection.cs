using Microsoft.Extensions.DependencyInjection;
using NexaStore.Application.Features.Categories.CreateCategory;
using NexaStore.Application.Features.Categories.GetCategoryById;
using NexaStore.Application.Features.Categories.GetAllCategories;
using NexaStore.Application.Features.Categories.UpdateCategory;
using NexaStore.Application.Features.Categories.DeleteCategory;

namespace NexaStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<GetCategoryByIdHandler>();
        services.AddScoped<GetAllCategoriesHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<DeleteCategoryHandler>();

        return services;
    }
}