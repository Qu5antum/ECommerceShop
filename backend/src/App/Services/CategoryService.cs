using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Services.Caching;
using App.Transactions;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface ICategoryService
{
    Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto);
    Task<List<CategoryResponseDto>> GetAllCategoriesAsync();
    Task<CategoryResponseDto> GetCategoryByIdAsync(Guid categoryId);
    Task<bool> UpdateCategoryByIdAsync(Guid categoryId, CategoryUpdateDto categoryUpdateDto);
    Task<bool> DeleteCategoryByIdAsync(Guid categoryId);
}


public class CategoryService : ICategoryService
{
    private readonly ICategoryRepsitory _repository;
    private readonly ILogger<CategoryService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCacheService _cache;

    private static string GetCategoryCacheKey(Guid categoryId)
    {
        return $"category:{categoryId}";
    }

    public CategoryService(ICategoryRepsitory repsitory, ILogger<CategoryService> logger, IHelperService helper, IUnitOfWork unitOfWork, IRedisCacheService cache)
    {
        _repository = repsitory;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto)
    {
        await _unitOfWork.BeginTransactionAsync();

        var categoryWithTitle = await _repository.GetCategoryByTitle(categoryCreateDto.Title);

        if (categoryWithTitle != null)
        {
            _logger.LogWarning("Category with this Title already exists: {Title}", categoryCreateDto.Title);
            throw new AlreadyExistsException("Category with this title already exists");
        }

        var categoryWithSlug = await _repository.GetCategoryBySlug(categoryCreateDto.Slug);

        if (categoryWithSlug != null)
        {
            _logger.LogWarning("Category with this Slug already exists: {Slug}", categoryCreateDto.Slug);
            throw new AlreadyExistsException("Category with this slug already exists");
        }

        try
        {
            var newCategory = new Category
            {
                Title = categoryCreateDto.Title,
                Slug = categoryCreateDto.Slug
            };

            await _repository.CreateAsync(newCategory);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            
            _logger.LogInformation("Category successfully created");

            await _cache.RemoveDataAsync("categories:all");

            _logger.LogInformation("Categories deleted from redis cache");

            return new CategoryResponseDto
            {
                Id = newCategory.Id,
                Title = newCategory.Title,
                Slug = newCategory.Slug,
                CreatedAt = newCategory.CreatedAt,
                UpdatedAt = newCategory.UpdatedAt
            };
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while creating the category: {Message}.", ex.Message);
            throw new DatabaseException("Could not save the category to the database.");
        }
    }

    public async Task<List<CategoryResponseDto>> GetAllCategoriesAsync()
    {
        var cacheKey = $"categories:all";

        var cachedCategories = await _cache.GetDataAsync<List<CategoryResponseDto>>(cacheKey);

        if (cachedCategories is not null)
        {
            _logger.LogInformation("Categories retrieved from redis cache");

            return cachedCategories;
        }

        var categories = await _repository.GetAllAsync();

        var result = categories.Select(category => new CategoryResponseDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        }).ToList();

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("Successfull response of categories");

        return result;
    }

    public async Task<bool> UpdateCategoryByIdAsync(Guid categoryId, CategoryUpdateDto categoryUpdateDto)
    {
        await _unitOfWork.BeginTransactionAsync();

        var category = await _helper.GetCategoryOr404(categoryId);

        if (categoryUpdateDto.Title != null)
        {
            category.Title = categoryUpdateDto.Title;
        }
        if (categoryUpdateDto.Slug != null)
        {
            category.Slug = categoryUpdateDto.Slug;
        }

        try
        {
            category.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Category successfully updated: {categoryId}", categoryId);

            await _cache.RemoveDataAsync("categories:all");
            await _cache.RemoveDataAsync(GetCategoryCacheKey(categoryId));

            _logger.LogInformation("Categories deleted from redis cache");

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while updating the category: {Message}.", ex.Message);
            throw new DatabaseException("Could not update the category to the database.");
        }
    }

    public async Task<bool> DeleteCategoryByIdAsync(Guid categoryId)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        var category = await _helper.GetCategoryOr404(categoryId);

        try
        {
            await _repository.DeleteAsync(category);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Category successfully updated: {categoryId}", categoryId);

            await _cache.RemoveDataAsync("categories:all");
            await _cache.RemoveDataAsync(GetCategoryCacheKey(categoryId));

            _logger.LogInformation("Categories deleted from redis cache");

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Database error while deleting category: {Message}", ex.Message);
            throw new DatabaseException("Database error while deleting category");
        }
        
    }

    public async Task<CategoryResponseDto> GetCategoryByIdAsync(Guid categoryId)
    {
        var cacheKey = GetCategoryCacheKey(categoryId);

        var cachedCategory = await _cache.GetDataAsync<CategoryResponseDto>(cacheKey);

        if (cachedCategory is not null)
        {
            _logger.LogInformation("Category retrieved from redis cache");
            return cachedCategory;
        }

        var category = await _helper.GetCategoryOr404(categoryId);

        var result = new CategoryResponseDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("Successfull response of category: {categoryId}", categoryId);

        return result;
    }
}