using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; 

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;


public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<ILogger<CategoryService>> _loggerMock;
    private readonly Mock<IHelperService> _helper;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _helper = new Mock<IHelperService>();
        _loggerMock = new Mock<ILogger<CategoryService>>();

        _categoryService = new CategoryService(
            _categoryRepositoryMock.Object,
            _loggerMock.Object,
            _helper.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task CreateCategory_ReturnsOk()
    {
        var request = new CategoryCreateDto
        {
            Title = "Electronics",
            Slug = "electronics"
        };

        _categoryRepositoryMock.Setup(r => r.GetCategoryByTitle(request.Title))
            .ReturnsAsync((Category?)null);

        _categoryRepositoryMock.Setup(r => r.GetCategoryBySlug(request.Slug))
            .ReturnsAsync((Category?)null);

        var result = await _categoryService.CreateCategoryAsync(request);

        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);
        result.Slug.Should().Be(request.Slug);

        _categoryRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync("categories:all"), Times.Once);
    }

    [Fact]
    public async Task CreateCategory_WithExistingTitle_ThrowsAlreadyExistsException()
    {
        var TitleExistingCategory = CategoryTestHelper.CreateTestCategory(
            Title: "TestTitle"
        );

        _categoryRepositoryMock.Setup(r => r.GetCategoryByTitle(TitleExistingCategory.Title))
            .ReturnsAsync(TitleExistingCategory);
        
        var request = new CategoryCreateDto
        {
            Title = TitleExistingCategory.Title,
            Slug = TitleExistingCategory.Slug
        };

        Func<Task> action = async () => await _categoryService.CreateCategoryAsync(request);

        await action.Should().ThrowAsync<AlreadyExistsException>()
            .WithMessage("Category with this title already exists");

        _categoryRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateCategory_WithExistingSlug_ThrowsAlreadyExistsException()
    {
        var SlugExistingCategory = CategoryTestHelper.CreateTestCategory(
            Slug: "TestSlug"
        );

        _categoryRepositoryMock.Setup(r => r.GetCategoryByTitle(SlugExistingCategory.Title))
            .ReturnsAsync((Category?)null);

        _categoryRepositoryMock.Setup(r => r.GetCategoryBySlug(SlugExistingCategory.Slug))
            .ReturnsAsync(SlugExistingCategory);

        var request = new CategoryCreateDto
        {
            Title = "Electronic",
            Slug = SlugExistingCategory.Slug
        };

        Func<Task> action = async () => await _categoryService.CreateCategoryAsync(request);

        await action.Should().ThrowAsync<AlreadyExistsException>()
            .WithMessage("Category with this slug already exists");

        _categoryRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task CategoryUpdate_ReturnsOk()
    {
        var CategorytoUpdate = CategoryTestHelper.CreateTestCategory();

        _helper.Setup(h => h.GetCategoryOr404(CategorytoUpdate.Id))
            .ReturnsAsync(CategorytoUpdate);

        var request = new CategoryUpdateDto
        {
            Title = "Electronic",
            Slug = "electronic"
        };

        var result = await _categoryService.UpdateCategoryByIdAsync(CategorytoUpdate.Id, request);

        result.Should().BeTrue();

        _categoryRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync("categories:all"), Times.Once);
        _cacheMock.Verify(c => c.RemoveDataAsync($"category:{CategorytoUpdate.Id}"), Times.Once);
    }

    [Fact]
    public async Task UpdateCategory_WhenCategoryDoesNotExist_ThrowsNotFoundException()
    {
        var categoryId = Guid.NewGuid();
        
        var updateDto = new CategoryUpdateDto
        {
            Title = "New Title",
            Slug = "new-title"
        };

        _helper.Setup(h => h.GetCategoryOr404(categoryId))
            .ThrowsAsync(new NotFoundException("Category not found"));

        Func<Task> action = async () => await _categoryService.UpdateCategoryByIdAsync(categoryId, updateDto);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Category not found");

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteCategory_ReturnOk()
    {
        var CategoryToDelete = CategoryTestHelper.CreateTestCategory();

        _helper.Setup(h => h.GetCategoryOr404(CategoryToDelete.Id))
            .ReturnsAsync(CategoryToDelete);

        var result = await _categoryService.DeleteCategoryByIdAsync(CategoryToDelete.Id);

        result.Should().BeTrue();

        _categoryRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync("categories:all"), Times.Once);
        _cacheMock.Verify(c => c.RemoveDataAsync($"category:{CategoryToDelete.Id}"), Times.Once);
    }
    
    [Fact]
    public async Task DeleteCategory_WhenCategoryDoesNotExists_ThrowsNotFoundException()
    {
        var categoryId = Guid.NewGuid();

        _helper.Setup(h => h.GetCategoryOr404(categoryId))
            .ThrowsAsync(new NotFoundException("Category not found"));
    
        Func<Task> action = async () => await _categoryService.DeleteCategoryByIdAsync(categoryId);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Category not found");

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WhenCacheIsEmpty_ReturnsCategoriesFromRepositoryAndCachesThem()
    {
        var categoriesFromRepo = new List<Category>
        {
            CategoryTestHelper.CreateTestCategory(Title: "Electronics", Slug: "electronics"),
            CategoryTestHelper.CreateTestCategory(Title: "Clothing", Slug: "clothing")
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<CategoryResponseDto>>("categories:all"))
            .ReturnsAsync((List<CategoryResponseDto>?)null);

        _categoryRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(categoriesFromRepo);

        var result = await _categoryService.GetAllCategoriesAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Title == "Electronics");
        result.Should().Contain(c => c.Title == "Clothing");

        _cacheMock.Verify(c => c.SetDataAsync(
            "categories:all", 
            It.Is<List<CategoryResponseDto>>(list => list.Count == 2), 
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_WhenCacheExists_ReturnsCachedCategoriesWithoutCallingRepository()
    {
        var cachedCategories = new List<CategoryResponseDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Cached Category", Slug = "cached-category" }
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<CategoryResponseDto>>("categories:all"))
            .ReturnsAsync(cachedCategories);

        var result = await _categoryService.GetAllCategoriesAsync();

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedCategories);

        _categoryRepositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
    }
}