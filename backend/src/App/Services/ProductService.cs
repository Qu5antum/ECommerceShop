using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Transactions;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface IProductService
{
    Task<ProductResponseDto> CreateProductAsync(Guid userId, ProductCreateDto productCreateDto);
    Task<bool> UpdateProductAsync(Guid userId, Guid productId, ProductUpdateDto productUpdateDto);
    Task<bool> DeleteProductAsync(Guid userId, Guid productId);
    Task<ProductResponseDto> GetProductByIdAsync(Guid productId);
    Task<List<ProductResponseDto>> GetProductsAsync();
    Task<(Stream FileStream, string ContentType)?> GetProductImageAsync(Guid productId);
    Task<List<ProductResponseDto>> GetProductsByCategoryIdAsync(Guid categoryId);
    Task<List<ProductResponseDto>> SearchProductAsync(string productName);
    Task<List<ProductResponseDto>> SearchProductByPriceDesc(string productName);
    Task<List<ProductResponseDto>> SearchProductByPriceAsc(string productName);
}


public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ISellerProfileRepository _profileRepository;
    private readonly IFileStorageService _fileService;
    private readonly ILogger<ProductService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService
    (
        IProductRepository productRepository,
        ISellerProfileRepository profileRepository,
        IFileStorageService fileService,
        ILogger<ProductService> logger,
        IHelperService helper,
        IUnitOfWork unitOfWork
    )
    {
        _productRepository = productRepository;
        _profileRepository = profileRepository;
        _fileService = fileService;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponseDto> CreateProductAsync(Guid userId, ProductCreateDto productCreateDto)
    {
        await _unitOfWork.BeginTransactionAsync();

        string? ImageUrl = null;

        await _helper.GetUserOr404(userId);

        await _helper.GetCategoryOr404(productCreateDto.categoryId);

        var sellerProfile = await _profileRepository.GetSellerProfileByUserIdAsync(userId);

        if (sellerProfile == null)
        {
            _logger.LogWarning("Seller profile not found of this user: {userId}", userId);
            throw new NotFoundException("Seller profile not found, you can't add product");
        }

        try
        {
            if (productCreateDto.Image != null && productCreateDto.Image.Length > 0)
            {
                ImageUrl = await _fileService.UploadFileAsync(productCreateDto.Image);
            }

            var newProduct = new Product
            {
                Name = productCreateDto.Name,
                Description = productCreateDto.Description,
                Price = productCreateDto.Price,
                SKU = productCreateDto.SKU,
                Stock = productCreateDto.Stock,
                ImageUrl = ImageUrl,
                SellerProfileId = sellerProfile.Id,
                CategoryId = productCreateDto.categoryId
            };

            await _productRepository.CreateAsync(newProduct);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

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
                ImageUrl = newProduct.ImageUrl,
                CreatedAt = newProduct.CreatedAt,
                UpdatedAt = newProduct.UpdatedAt
            };
        }
        catch (DbUpdateException ex)
        {
            if (ImageUrl != null)
            {
                await _fileService.DeleteFileAsync(ImageUrl);
            }

            await _unitOfWork.RollbackAsync();

            _logger.LogError(
                 ex,
                 "Database error while creating product. Inner exception: {Message}",
                 ex.InnerException?.Message
             );

            throw new DatabaseException("Could not save the product to the database.");
        }
    }

    public async Task<bool> UpdateProductAsync(Guid userId, Guid productId, ProductUpdateDto productUpdateDto)
    {
        await _unitOfWork.BeginTransactionAsync();

        string? newImageUrl = null;

        await _helper.GetUserOr404(userId);

        if (productUpdateDto.categoryId.HasValue)
        {
            await _helper.GetCategoryOr404(productUpdateDto.categoryId.Value);
        }

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

        string? oldImageUrl = product.ImageUrl;

        try
        {
            if (productUpdateDto.Image != null && productUpdateDto.Image.Length > 0)
            {
                newImageUrl = await _fileService.UploadFileAsync(productUpdateDto.Image);

                product.ImageUrl = newImageUrl;
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

            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            if (newImageUrl != null && oldImageUrl != null)
            {
                await _fileService.DeleteFileAsync(oldImageUrl);
            }

            _logger.LogInformation("Product successfully updated");

            return true;
        }
        catch (DbUpdateException ex)
        {
            if (newImageUrl != null)
            {
                await _fileService.DeleteFileAsync(newImageUrl);
            }

            await _unitOfWork.RollbackAsync();

            _logger.LogError(
                ex,
                "Database error while updating product {ProductId}. Inner exception: {Message}",
                productId,
                ex.InnerException?.Message
            );

            throw new DatabaseException("Could not update the product to the database.");
        }
    }

    public async Task<bool> DeleteProductAsync(Guid userId, Guid productId)
    {
        await _unitOfWork.BeginTransactionAsync();
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

        if (product.ImageUrl != null)
        {
            await _fileService.DeleteFileAsync(product.ImageUrl);
        }

        try
        {
            await _productRepository.DeleteAsync(product);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Product successfully deleted");

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Database error while deleting product: {Message}", ex.Message);
            throw new DatabaseException("Database error while deleting product");
        }
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
            ImageUrl = product.ImageUrl,
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
            ImageUrl = product.ImageUrl,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        }).ToList();
    }

    public async Task<(Stream FileStream, string ContentType)?> GetProductImageAsync(Guid productId)
    {
        var product = await _helper.GetProductOr404(productId);

        if (string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            return null;
        }

        return await _fileService.GetFileAsync(product.ImageUrl);
    }

    public async Task<List<ProductResponseDto>> GetProductsByCategoryIdAsync(Guid categoryId)
    {
        await _helper.GetCategoryOr404(categoryId);

        var products = await _productRepository.GetProductsByCategoryIdAsync(categoryId);

        _logger.LogInformation("Successfull response of products, categoryID: {categoryId}", categoryId);

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
            ImageUrl = product.ImageUrl,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        }).ToList();
    }

    public async Task<List<ProductResponseDto>> SearchProductAsync(string productName)
    {
        var products = await _productRepository.SearchProductByNameAsync(productName);

        _logger.LogInformation("Successfull response of products, product name: {productName}", productName);

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
            ImageUrl = product.ImageUrl,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        }).ToList();
    }

    public async Task<List<ProductResponseDto>> SearchProductByPriceDesc(string productName)
    {
        var products = await _productRepository.SearchProductByPriceDesc(productName);

        _logger.LogInformation("Successfull response of products, product name: {productName}", productName);

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
            ImageUrl = product.ImageUrl,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        }).ToList();
    }

    public async Task<List<ProductResponseDto>> SearchProductByPriceAsc(string productName)
    {
        var products = await _productRepository.SearchProductByPriceAsc(productName);

        _logger.LogInformation("Successfull response of products, product name: {productName}", productName);

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
            ImageUrl = product.ImageUrl,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        }).ToList();
    }
}