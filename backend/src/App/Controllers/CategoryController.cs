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
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateCategory(CategoryCreateDto categoryCreateDto)
    {
        var category = await _service.CreateCategoryAsync(categoryCreateDto);

        return Ok(category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, CategoryUpdateDto categoryUpdateDto)
    {
        await _service.UpdateCategoryByIdAsync(categoryId, categoryUpdateDto);

        return Ok("Category successfully updated");
    }
    
    [Authorize(Roles = "Admin")]
    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteCategory(Guid categoryId)
    {
        await _service.DeleteCategoryByIdAsync(categoryId);

        return Ok("Category successfully delelted");
    }
    
    [Authorize]
    [HttpGet("Categories")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _service.GetAllCategoriesAsync();

        return Ok(categories);
    }

    [Authorize]
    [HttpGet("{categoryId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCategoryById(Guid categoryId)
    {
        var category = await _service.GetCategoryByIdAsync(categoryId);

        return Ok(category);
    }
}