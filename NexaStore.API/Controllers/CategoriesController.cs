using Microsoft.AspNetCore.Mvc;
using NexaStore.Application.Features.Categories.CreateCategory;
using NexaStore.Application.Features.Categories.DTOs;
using NexaStore.Application.Features.Categories.GetCategoryById;
using NexaStore.Application.Features.Categories.GetAllCategories;
using NexaStore.Application.Features.Categories.UpdateCategory;
using NexaStore.Application.Features.Categories.DeleteCategory;

namespace NexaStore.API.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly CreateCategoryHandler _handler;
    private readonly GetCategoryByIdHandler _getCategoryByIdHandler;
    private readonly GetAllCategoriesHandler _getAllCategoriesHandler;
    private readonly UpdateCategoryHandler _updateCategoryHandler;
    private readonly DeleteCategoryHandler _deleteCategoryHandler;

    public CategoriesController(CreateCategoryHandler handler,
     GetCategoryByIdHandler getCategoryByIdHandler,
     GetAllCategoriesHandler getAllCategoriesHandler,
     UpdateCategoryHandler updateCategoryHandler,
     DeleteCategoryHandler deleteCategoryHandler)
    {
        _handler = handler;
        _getCategoryByIdHandler = getCategoryByIdHandler;
        _getAllCategoriesHandler = getAllCategoriesHandler;
        _updateCategoryHandler = updateCategoryHandler;
        _deleteCategoryHandler = deleteCategoryHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _handler.Handle(
            request,
            cancellationToken
        );

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new { id = result.Value }
        );
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getCategoryByIdHandler.Handle(
            id,
            cancellationToken
        );

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _getAllCategoriesHandler.Handle(
            cancellationToken
        );

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateCategoryHandler.Handle(
            id,
            request,
            cancellationToken
        );

        if (result.IsFailure)
            return NotFound(result.Error);

        return NoContent();
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _deleteCategoryHandler.Handle(
            id,
            cancellationToken
        );

        if (result.IsFailure)
            return NotFound(result.Error);

        return NoContent();
    }
}