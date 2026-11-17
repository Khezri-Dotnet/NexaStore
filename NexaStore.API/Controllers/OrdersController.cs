
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexaStore.Application.Features.Orders.CreateOrder;
using NexaStore.Application.Features.Orders.GetMyOrders;
using NexaStore.Application.Features.Orders.GetOrderById;

namespace NexaStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly CreateOrderHandler _createOrderHandler;
    private readonly GetOrderByIdHandler _getOrderByIdHandler;
    private readonly GetMyOrdersHandler _getMyOrdersHandler;

    public OrdersController(
        CreateOrderHandler createOrderHandler,
        GetOrderByIdHandler getOrderByIdHandler,
        GetMyOrdersHandler getMyOrdersHandler)
    {
        _createOrderHandler = createOrderHandler;
        _getOrderByIdHandler = getOrderByIdHandler;
        _getMyOrdersHandler = getMyOrdersHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var customerId))
            return Unauthorized();

        var result = await _createOrderHandler.Handle(
            customerId,
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = result.Value },
            new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var customerId))
            return Unauthorized();

        var result = await _getOrderByIdHandler.Handle(
            id,
            customerId,
            cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders(
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var customerId))
            return Unauthorized();

        var result = await _getMyOrdersHandler.Handle(
            customerId,
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
