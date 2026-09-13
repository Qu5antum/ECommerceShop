using App.DTOs;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryContoller : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoryContoller(ICategoryService service)
    {
        _service = service;
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateCategory(CategoryCreateDto categoryCreateDto)
    {
        var category = await _service.CreateCategoryAsync(categoryCreateDto);

        return Ok(category);
    }
    
    [Authorize]
    [HttpGet("categories")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _service.GetAllCategoriesAsync();

        return Ok(categories);
    }

    [Authorize]
    [HttpGet("{categoryId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCategoryById(Guid categoryId)
    {
        var category = await _service.GetCategoryByIdAsync(categoryId);

        return Ok(category);
    }
}