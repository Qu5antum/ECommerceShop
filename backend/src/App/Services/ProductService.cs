using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface IProductService
{
    Task<ProductResponseDto> CreateProductAsync(Guid userId, Guid categoryId, ProductCreateDto productCreateDto);
    Task<bool> UpdateProductAsync(Guid userId, Guid categoryId, Guid productId, ProductUpdateDto productUpdateDto);
    Task<bool> DeleteProductAsync(Guid userId, Guid productId);
    Task<ProductResponseDto> GetProductByIdAsync(Guid productId);
    Task<List<ProductResponseDto>> GetProductsAsync();
}


public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ISellerProfileRepository _profileRepository;
    private readonly IFileStorageService _fileService;
    private readonly ILogger<ProductService> _logger;
    private readonly IHelper _helper;

    public ProductService
    (
        IProductRepository productRepository,
        ISellerProfileRepository profileRepository,
        IFileStorageService fileService,
        ILogger<ProductService> logger,
        IHelper helper
    )
    {
        _productRepository = productRepository;
        _profileRepository = profileRepository;
        _fileService = fileService;
        _logger = logger;
        _helper = helper;
    }

    public async Task<ProductResponseDto> CreateProductAsync(Guid userId, Guid categoryId, ProductCreateDto productCreateDto)
    {
        string? ImageUrl = null;

        await _helper.GetUserOr404(userId);

        await _helper.GetCategoryOr404(categoryId);

        var sellerProfile = await _profileRepository.GetSellerProfileByUserIdAsync(userId);

        if (sellerProfile == null)
        {
            _logger.LogWarning("Seller profile not found of this user: {userId}", userId);
            throw new NotFoundException("Seller profile not found, you can't add product");
        }

        if (productCreateDto.Image != null && productCreateDto.Image.Length > 0)
        {
            ImageUrl = await _fileService.UploadFileAsync(productCreateDto.Image);
        }

        try
        {
            var newProduct = new Product
            {
                Name = productCreateDto.Name,
                Description = productCreateDto.Description,
                Price = productCreateDto.Price,
                SKU = productCreateDto.SKU,
                Stock = productCreateDto.Stock,
                ImageUrl = ImageUrl,
                SellerProfileId = sellerProfile.Id,
                CategoryId = categoryId
            };

            await _productRepository.CreateAsync(newProduct);

            _logger.LogInformation("Product successfully create: {productId}", newProduct.Id);

            return new ProductResponseDto
            {
                Id = newProduct.Id,
                SellerProfileId = newProduct.SellerProfileId,
                CategoryId = newProduct.CategoryId,
                Name = newProduct.Name,
                Description = newProduct.Description,
                Price = newProduct.Price,
                SKU = newProduct.SKU,
                Stock = newProduct.Stock,
                CreatedAt = newProduct.CreatedAt,
                UpdatedAt = newProduct.UpdatedAt
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "A database error occurred while creating the product.");
            throw new DatabaseException("Could not save the product to the database.");
        }
    }

    public async Task<bool> UpdateProductAsync(Guid userId, Guid categoryId, Guid productId, ProductUpdateDto productUpdateDto)
    {
        string? ImageUrl = null;

        await _helper.GetUserOr404(userId);

        await _helper.GetCategoryOr404(categoryId);

        var sellerProfile = await _profileRepository.GetSellerProfileByUserIdAsync(userId);

        if (sellerProfile == null)
        {
            _logger.LogWarning("Seller profile not found of this user: {userId}", userId);
            throw new NotFoundException("Seller profile not found, you can't update product");
        }

        var product = await _helper.GetProductOr404(productId);

        if (product.SellerProfileId != sellerProfile.Id)
        {
            _logger.LogWarning("Product does not belong to this seller profile: profile ID: {profileId}, product ID: {productId}", sellerProfile.Id, productId);
            throw new BadRequestException("Product does not belong to seller");
        }

        if (productUpdateDto.Name != null)
        {
            product.Name = productUpdateDto.Name;
        }
        if (productUpdateDto.Description != null)
        {
            product.Description = productUpdateDto.Description;
        }
        if (productUpdateDto.Price.HasValue)
        {
            product.Price = productUpdateDto.Price.Value;
        }
        if (productUpdateDto.SKU != null)
        {
            product.SKU = productUpdateDto.SKU;
        }
        if (productUpdateDto.Stock.HasValue)
        {
            product.Stock = productUpdateDto.Stock.Value;
        }

        if (productUpdateDto.Image != null && productUpdateDto.Image.Length > 0 && product.ImageUrl != null)
        {
            await _fileService.DeleteFileAsync(product.ImageUrl);
            ImageUrl = await _fileService.UploadFileAsync(productUpdateDto.Image);
        }

        else if (productUpdateDto.Image != null && productUpdateDto.Image.Length > 0 && product.ImageUrl == null)
        {
            ImageUrl = await _fileService.UploadFileAsync(productUpdateDto.Image);
        }
        
        product.ImageUrl = ImageUrl;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);

        _logger.LogInformation("Product successfully updated");

        return true;
    }

    public async Task<bool> DeleteProductAsync(Guid userId, Guid productId)
    {
        await _helper.GetUserOr404(userId);

        var sellerProfile = await _profileRepository.GetSellerProfileByUserIdAsync(userId);

        if (sellerProfile == null)
        {
            _logger.LogWarning("Seller profile not found of this user: {userId}", userId);
            throw new NotFoundException("Seller profile not found, you can't delete product");
        }

        var product = await _helper.GetProductOr404(productId);

        if (product.SellerProfileId != sellerProfile.Id)
        {
            _logger.LogWarning("Product does not belong to this seller profile: profile ID: {profileId}, product ID: {productId}", sellerProfile.Id, productId);
            throw new BadRequestException("Product does not belong to seller");
        }

        await _productRepository.DeleteAsync(product);

        _logger.LogInformation("Product successfully deleted");

        return true;
    }

    public async Task<ProductResponseDto> GetProductByIdAsync(Guid productId)
    {
        var product = await _helper.GetProductOr404(productId);

        _logger.LogInformation("Product successfull response");

        return new ProductResponseDto
        {
            Id = product.Id,
            SellerProfileId = product.SellerProfileId,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            SKU = product.SKU,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task<List<ProductResponseDto>> GetProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();

        _logger.LogInformation("Products successfull response");

        return products.Select(product => new ProductResponseDto
        {
            Id = product.Id,
            SellerProfileId = product.SellerProfileId,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            SKU = product.SKU,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        }).ToList();
    }
}