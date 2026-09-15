using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
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

    public CategoryService(ICategoryRepsitory repsitory, ILogger<CategoryService> logger, IHelperService helper)
    {
        _repository = repsitory;
        _logger = logger;
        _helper = helper;
    }

    public async Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateDto categoryCreateDto)
    {
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

            _logger.LogInformation("Category successfully created");

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
            _logger.LogError(ex, "A database error occurred while creating the category.");
            throw new DatabaseException("Could not save the category to the database.");
        }
    }

    public async Task<List<CategoryResponseDto>> GetAllCategoriesAsync()
    {
        var categories = await _repository.GetAllAsync();

        _logger.LogInformation("Successfull response of categories");

        return categories.Select(category => new CategoryResponseDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        }).ToList();
    }

    public async Task<bool> UpdateCategoryByIdAsync(Guid categoryId, CategoryUpdateDto categoryUpdateDto)
    {
        var category = await _helper.GetCategoryOr404(categoryId);

        if (categoryUpdateDto.Title != null)
        {
            category.Title = categoryUpdateDto.Title;
        }
        if (categoryUpdateDto.Slug != null)
        {
            category.Slug = categoryUpdateDto.Slug;
        }

        category.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(category);

        _logger.LogInformation("Category successfully updated: {categoryId}", categoryId);

        return true;
    }

    public async Task<bool> DeleteCategoryByIdAsync(Guid categoryId)
    {
        var category = await _helper.GetCategoryOr404(categoryId);

        await _repository.DeleteAsync(category);

        _logger.LogInformation("Category successfully updated: {categoryId}", categoryId);

        return true;
    }

    public async Task<CategoryResponseDto> GetCategoryByIdAsync(Guid categoryId)
    {
        var category = await _helper.GetCategoryOr404(categoryId);

        return new CategoryResponseDto
        {
            Id = category.Id,
            Title = category.Title,
            Slug = category.Slug,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}