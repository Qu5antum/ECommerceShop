using App.DTOs;
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
    public async Task<IActionResult> CreateProduct(ProductCreateDto productCreateDto)
    {
        Guid userId = _helper.GetUserId();

        var product = await _service.CreateProductAsync(userId, productCreateDto);

        return Ok(product);
    }

    [Authorize(Roles = "Seller")]
    [HttpPut("{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid productId, ProductUpdateDto productUpdateDto)
    {
        Guid userId = _helper.GetUserId();

        await _service.UpdateProductAsync(userId, productId, productUpdateDto);

        return Ok("Product updated successfully");
    }

    [Authorize(Roles = "Seller")]
    [HttpDelete("{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid productId)
    {
        Guid userId = _helper.GetUserId();

        await _service.DeleteProductAsync(userId, productId);

        return Ok("Product successfully deleted");
    }

    [Authorize]
    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetProductById(Guid productId)
    {
        var product = await _service.GetProductByIdAsync(productId);

        return Ok(product);
    }

    [Authorize]
    [HttpGet("Products")]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _service.GetProductsAsync();

        return Ok(products);
    }
    
    [Authorize]
    [HttpGet("{ProductId:guid}/image")]
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
    [HttpGet("Category/{categoryId:guid}")]
    public async Task<IActionResult> GetProductsByCategoryId(Guid categoryId)
    {
        var products = await _service.GetProductsByCategoryIdAsync(categoryId);

        return Ok(products);
    }

    [Authorize]
    [HttpGet("Search")]
    public async Task<IActionResult> SearchProduct(string productName)
    {
        var products = await _service.SearchProductAsync(productName);

        return Ok(products);
    }

    [Authorize]
    [HttpGet("Search/Min")]
    public async Task<IActionResult> SearchProductAsc(string productName)
    {
        var products = await _service.SearchProductByPriceAsc(productName);

        return Ok(products);
    }

    [Authorize]
    [HttpGet("Search/Max")]
    public async Task<IActionResult> SearchProductDesc(string productName)
    {
        var products = await _service.SearchProductByPriceDesc(productName);

        return Ok(products);
    }

    [Authorize(Roles = "Admin, Moderator")]
    [HttpDelete("Admin/{productId:guid}")]
    public async Task<IActionResult> DeleteProductAdmin(Guid productId)
    {
        await _service.DeleteProductByIdAdminAsync(productId);

        return Ok("Product successfully deleted");
    }

    [Authorize(Roles = "Admin, Moderator")]
    [HttpGet("Admin")]
    public async Task<IActionResult> GetProductsOutOfStock()
    {
        var products = await _service.GetProductThatOutOfStockAsync();

        return Ok(products);
    }
}