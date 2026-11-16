
using Microsoft.EntityFrameworkCore;
using NexaStore.Infrastructure;
using NexaStore.Infrastructure.Persistence;
using NexaStore.Application.Features.Products.CreateProduct;
using NexaStore.Application.Features.Products.GetProductById;
using NexaStore.Application.Features.Products.GetAllProducts;
using NexaStore.Application.Features.Products.UpdateProduct;
using NexaStore.Application.Features.Products.DeleteProduct;
using NexaStore.Application.Features.Products.DTOs;
using NexaStore.Application.Common.Results;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddInfrastructure(builder.Configuration);
// Application Handlers
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<GetProductByIdHandler>();
builder.Services.AddScoped<GetAllProductsHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<DeleteProductHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Health Check
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Application = "NexaStore"
    });
});

// Database Health Check
app.MapGet("/api/health/database", async (
    NexaStoreDbContext db,
    CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(
        cancellationToken
    );

    if (!canConnect)
    {
        return Results.Problem(
            title: "Database connection failed",
            statusCode: 503
        );
    }

    return Results.Ok(new
    {
        Status = "Healthy",
        Database = "Connected",
        Provider = "PostgreSQL"
    });
});

app.MapPost("/api/products", async (
    CreateProductRequest request,
    CreateProductHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        request,
        cancellationToken
    );

    // Handle errors
    if (result.IsFailure)
    {
        var statusCode = result.Error!.Code == "Category.NotFound"
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: statusCode
        );
    }

    // Return 201 Created
    var product = result.Value!;

    return Results.Created(
        $"/api/products/{product.Id}",
        product
    );
})
.WithName("CreateProduct")
.WithTags("Products")
.Produces<ProductResponse>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound);


app.MapGet("/api/products", async (
    GetAllProductsHandler handler,
    int pageNumber = 1,
    int pageSize = 10,
    string? search = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    CancellationToken cancellationToken = default) =>
{
    var query = new GetAllProductsQuery
    {
        PageNumber = pageNumber,
        PageSize = pageSize,
        Search = search,
        MinPrice = minPrice,
        MaxPrice = maxPrice
    };

    if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        return Results.BadRequest(new { message = "Invalid pagination parameters." });

    if (minPrice < 0 || maxPrice < 0 ||
        (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice))
        return Results.BadRequest(new { message = "Invalid price range." });

    var result = await handler.HandleAsync(query, cancellationToken);

    if (result.IsFailure)
    {
        return Results.Problem(
            title: result.Error!.Code,
            detail: result.Error.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    return Results.Ok(result.Value);
})
.WithName("GetAllProducts")
.WithTags("Products")
.Produces<PagedResult<ProductResponse>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status500InternalServerError);



app.MapGet("/api/products/{id}", async (
    Guid id,
    GetProductByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        id,
        cancellationToken
    );

    if (result.IsFailure)
    {
        var statusCode = result.Error!.Code == "Product.NotFound"
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status500InternalServerError;

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: statusCode
        );
    }

    return Results.Ok(result.Value);
})
.WithName("GetProductById")
.WithTags("Products")
.Produces<ProductResponse>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPut("/api/products/{id:guid}", async (
    Guid id,
    UpdateProductRequest request,
    UpdateProductHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        id,
        request,
        cancellationToken
    );

    if (result.IsFailure)
    {
        var statusCode = result.Error!.Code switch
        {
            "Product.NotFound" => StatusCodes.Status404NotFound,
            "Category.NotFound" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: statusCode
        );
    }

    return Results.Ok(result.Value);
})
.WithName("UpdateProduct")
.WithTags("Products")
.Produces<ProductResponse>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound);




app.MapDelete("/api/products/{id:guid}", async (
    Guid id,
    DeleteProductHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        id,
        cancellationToken
    );

    if (result.IsFailure)
    {
        var statusCode = result.Error!.Code switch
        {
            "Product.NotFound" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: statusCode
        );
    }

    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithTags("Products")
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound);



app.Run();
