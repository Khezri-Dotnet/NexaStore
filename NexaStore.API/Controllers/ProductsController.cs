
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using NexaStore.Application.Common.Results;
using NexaStore.Application.Features.Products.CreateProduct;
using NexaStore.Application.Features.Products.GetProductById;
using NexaStore.Application.Features.Products.GetAllProducts;
using NexaStore.Application.Features.Products.UpdateProduct;
using NexaStore.Application.Features.Products.DeleteProduct;
using NexaStore.Application.Features.Products.DTOs;

namespace NexaStore.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly CreateProductHandler _createHandler;
    private readonly GetProductByIdHandler _getByIdHandler;
    private readonly GetAllProductsHandler _getAllHandler;
    private readonly UpdateProductHandler _updateHandler;
    private readonly DeleteProductHandler _deleteHandler;

    public ProductsController(
        CreateProductHandler createHandler,
        GetProductByIdHandler getByIdHandler,
        GetAllProductsHandler getAllHandler,
        UpdateProductHandler updateHandler,
        DeleteProductHandler deleteHandler)
    {
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
        _getAllHandler = getAllHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(
            request, cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error, 400);

        var product = result.Value!;

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllProductsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.PageNumber < 1 ||
            query.PageSize < 1 ||
            query.PageSize > 100)
        {
            return BadRequest(new
            {
                message = "Invalid pagination parameters."
            });
        }

        if (query.MinPrice < 0 ||
            query.MaxPrice < 0 ||
            (query.MinPrice.HasValue &&
             query.MaxPrice.HasValue &&
             query.MinPrice > query.MaxPrice))
        {
            return BadRequest(new
            {
                message = "Invalid price range."
            });
        }

        var result = await _getAllHandler.HandleAsync(
            query, cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error, 500);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(
            id, cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error, 500);

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateHandler.HandleAsync(
            id, request, cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error, 400);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _deleteHandler.HandleAsync(
            id, cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error, 400);

        return NoContent();
    }

    private IActionResult HandleError(
        Error? error,
        int defaultStatusCode)
    {
        if (error is null)
            return StatusCode(defaultStatusCode);

        var statusCode = error.Code switch
        {
            "Product.NotFound" => 404,
            "Category.NotFound" => 404,
            "Validation.Failed" => 400,
            _ => defaultStatusCode
        };

        return Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode);
    }
}
