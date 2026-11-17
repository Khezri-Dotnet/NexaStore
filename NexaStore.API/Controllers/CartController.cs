
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaStore.Application.Features.Carts.DTOs;
using NexaStore.Application.Features.Carts.GetCart;
using NexaStore.Application.Features.Carts.AddCartItem;
using NexaStore.Application.Features.Carts.UpdateCartItem;
using NexaStore.Application.Features.Carts.RemoveCartItem;
using NexaStore.Application.Features.Carts.ClearCart;

namespace NexaStore.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public sealed class CartController : ControllerBase
{
    private readonly GetCartHandler _getHandler;
    private readonly AddCartItemHandler _addHandler;
    private readonly UpdateCartItemHandler _updateHandler;
    private readonly RemoveCartItemHandler _removeHandler;
    private readonly ClearCartHandler _clearHandler;

    public CartController(
        GetCartHandler getHandler,
        AddCartItemHandler addHandler,
        UpdateCartItemHandler updateHandler,
        RemoveCartItemHandler removeHandler,
        ClearCartHandler clearHandler)
    {
        _getHandler = getHandler;
        _addHandler = addHandler;
        _updateHandler = updateHandler;
        _removeHandler = removeHandler;
        _clearHandler = clearHandler;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _getHandler.HandleAsync(
            GetUserId(), cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(
        AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addHandler.HandleAsync(
            GetUserId(), request, cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(
        Guid itemId,
        UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateHandler.HandleAsync(
            GetUserId(), itemId, request, cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _removeHandler.HandleAsync(
            GetUserId(), itemId, cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(
        CancellationToken cancellationToken)
    {
        var result = await _clearHandler.HandleAsync(
            GetUserId(), cancellationToken);

        if (result.IsFailure)
            return HandleError(result.Error);

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var id))
            throw new UnauthorizedAccessException(
                "Invalid user identity.");

        return id;
    }

    private IActionResult HandleError(
        NexaStore.Application.Common.Results.Error? error)
    {
        if (error is null)
            return StatusCode(500);

        var statusCode = error.Code switch
        {
            "Product.NotFound" => 404,
            "Cart.NotFound" => 404,
            "Cart.ItemNotFound" => 404,
            "Cart.InvalidQuantity" => 400,
            "Cart.OperationFailed" => 400,
            _ => 400
        };

        return Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode);
    }
}
