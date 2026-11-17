
using System.Text;
using System.Security.Claims;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using NexaStore.Infrastructure;
using NexaStore.Infrastructure.Persistence;
using NexaStore.Infrastructure.Identity;

using NexaStore.Application;
using NexaStore.Application.Common.Results;

using NexaStore.Application.Features.Products.CreateProduct;
using NexaStore.Application.Features.Products.GetProductById;
using NexaStore.Application.Features.Products.GetAllProducts;
using NexaStore.Application.Features.Products.UpdateProduct;
using NexaStore.Application.Features.Products.DeleteProduct;
using NexaStore.Application.Features.Products.DTOs;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<GetProductByIdHandler>();
builder.Services.AddScoped<GetAllProductsHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<DeleteProductHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Email,

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Swagger + JWT
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

var app = builder.Build();

// Seed Roles
// using (var scope = app.Services.CreateScope())
// {
//     var roleManager = scope.ServiceProvider
//         .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

//     await RoleSeeder.SeedAsync(roleManager);
// }

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var adminEmail = builder.Configuration["Admin:Email"]
        ?? throw new InvalidOperationException("Admin email is missing.");

    await RoleSeeder.SeedAsync(
        roleManager,
        userManager,
        adminEmail);
}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Application = "NexaStore"
    });
})
.WithTags("Health");

// Database Health Check
app.MapGet("/api/health/database", async (
    NexaStoreDbContext db,
    CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(
        cancellationToken);

    if (!canConnect)
    {
        return Results.Problem(
            title: "Database connection failed",
            statusCode: 503);
    }

    return Results.Ok(new
    {
        Status = "Healthy",
        Database = "Connected",
        Provider = "PostgreSQL"
    });
})
.WithTags("Health");

// CREATE PRODUCT - Admin Only
app.MapPost("/api/products", async (
    CreateProductRequest request,
    [FromServices] CreateProductHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        request,
        cancellationToken);

    if (result.IsFailure)
    {
        var statusCode = result.Error!.Code == "Category.NotFound"
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: statusCode);
    }

    var product = result.Value!;

    return Results.Created(
        $"/api/products/{product.Id}",
        product);
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("CreateProduct")
.WithTags("Products")
.Produces<ProductResponse>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound);

// GET ALL PRODUCTS - Public
app.MapGet("/api/products", async (
    [FromServices] GetAllProductsHandler handler,
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
        return Results.BadRequest(new
        {
            message = "Invalid pagination parameters."
        });

    if (minPrice < 0 || maxPrice < 0 ||
        (minPrice.HasValue && maxPrice.HasValue &&
         minPrice > maxPrice))
    {
        return Results.BadRequest(new
        {
            message = "Invalid price range."
        });
    }

    var result = await handler.HandleAsync(
        query,
        cancellationToken);

    if (result.IsFailure)
    {
        return Results.Problem(
            title: result.Error!.Code,
            detail: result.Error.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }

    return Results.Ok(result.Value);
})
.WithName("GetAllProducts")
.WithTags("Products")
.Produces<PagedResult<ProductResponse>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status500InternalServerError);

// GET PRODUCT BY ID - Public
app.MapGet("/api/products/{id:guid}", async (
    Guid id,
    [FromServices] GetProductByIdHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        id,
        cancellationToken);

    if (result.IsFailure)
    {
        var statusCode = result.Error!.Code == "Product.NotFound"
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status500InternalServerError;

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: statusCode);
    }

    return Results.Ok(result.Value);
})
.WithName("GetProductById")
.WithTags("Products")
.Produces<ProductResponse>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status500InternalServerError);

// UPDATE PRODUCT - Admin Only
app.MapPut("/api/products/{id:guid}", async (
    Guid id,
    UpdateProductRequest request,
    [FromServices] UpdateProductHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        id,
        request,
        cancellationToken);

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
            statusCode: statusCode);
    }

    return Results.Ok(result.Value);
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("UpdateProduct")
.WithTags("Products")
.Produces<ProductResponse>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound);

// DELETE PRODUCT - Admin Only
app.MapDelete("/api/products/{id:guid}", async (
    Guid id,
    [FromServices] DeleteProductHandler handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        id,
        cancellationToken);

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
            statusCode: statusCode);
    }

    return Results.NoContent();
})
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.WithName("DeleteProduct")
.WithTags("Products")
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound);

app.Run();
