using App.DTOs;
using App.Models;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _service;
    private readonly Helper _helper;

    public ProductController(IProductService service, Helper helper)
    {
        _service = service;
        _helper = helper;
    }

    [Authorize(Roles = "Seller")]
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateProduct(ProductCreateDto productCreateDto)
    {
        Guid userId = _helper.GetUserId();

        var product = await _service.CreateProductAsync(userId, productCreateDto);

        return Ok(product);
    }

    [Authorize(Roles = "Seller")]
    [HttpPut("{productId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProduct(Guid productId, ProductUpdateDto productUpdateDto)
    {
        Guid userId = _helper.GetUserId();

        await _service.UpdateProductAsync(userId, productId, productUpdateDto);

        return Ok("Product updated successfully");
    }

    [Authorize(Roles = "Seller")]
    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteProduct(Guid productId)
    {
        Guid userId = _helper.GetUserId();

        await _service.DeleteProductAsync(userId, productId);

        return Ok("Product successfully deleted");
    }

    [Authorize]
    [HttpGet("{productId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProductById(Guid productId)
    {
        var product = await _service.GetProductByIdAsync(productId);

        return Ok(product);
    }

    [Authorize]
    [HttpGet("all")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _service.GetProductsAsync();

        return Ok(products);
    }
    
    [Authorize]
    [HttpGet("{productId:guid}/image")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProductImage(Guid productId)
    {
        var file = await _service.GetProductImageAsync(productId);

        if (file is null)
        {
            return NotFound("Product image not found.");
        }

        return File(file.Value.FileStream, file.Value.ContentType);
    }

    [Authorize]
    [HttpGet("category/{categoryId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProductsByCategoryId(Guid categoryId)
    {
        var products = await _service.GetProductsByCategoryIdAsync(categoryId);

        return Ok(products);
    }

    [Authorize]
    [HttpGet("search")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SearchProduct(string productName)
    {
        var products = await _service.SearchProductAsync(productName);

        return Ok(products);
    }
}