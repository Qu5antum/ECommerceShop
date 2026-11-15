using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;
using App.Enum;
using Microsoft.EntityFrameworkCore;


public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<ISellerProfileRepository> _sellerProfileRepositoryMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly Mock<IFileStorageService> _fileServiceMock;
    private readonly Mock<IHelperService> _helperMock;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _sellerProfileRepositoryMock = new Mock<ISellerProfileRepository>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _fileServiceMock = new Mock<IFileStorageService>();
        _helperMock = new Mock<IHelperService>();

        _productService = new ProductService(
            _productRepositoryMock.Object,
            _sellerProfileRepositoryMock.Object,
            _notificationRepositoryMock.Object,
            _fileServiceMock.Object,
            _loggerMock.Object,
            _helperMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task CreateProductAsync_WhenRequestIsValidWithImage_ReturnsCreatedProductAndUploadsFile()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Approved
        );

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);

        var createDto = new ProductCreateDto
        {
            categoryId = Guid.NewGuid(),
            Name = "Gaming Mouse",
            Description = "RGB Wireless Mouse",
            Price = 75.00m,
            SKU = "MS-RGB-01",
            Stock = 25,
            Image = fileMock.Object
        };

        var expectedFileUrl = "uploads/products/mouse.jpg";

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);
        _helperMock.Setup(h => h.GetCategoryOr404(createDto.categoryId)).ReturnsAsync(new Category()); 
        
        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        _fileServiceMock.Setup(f => f.UploadFileAsync(createDto.Image))
            .ReturnsAsync(expectedFileUrl);

        var result = await _productService.CreateProductAsync(testUser.Id, createDto);

        result.Should().NotBeNull();
        result.Name.Should().Be(createDto.Name);
        result.ImageUrl.Should().Be(expectedFileUrl);

        _fileServiceMock.Verify(f => f.UploadFileAsync(createDto.Image), Times.Once);
        _productRepositoryMock.Verify(r => r.CreateAsync(It.Is<Product>(p => 
            p.Name == createDto.Name && 
            p.SellerProfileId == testSellerProfile.Id &&
            p.ImageUrl == expectedFileUrl
        )), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateProductAsync_WhenSellerProfileNotApproved_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Pending
        );

        var createDto = new ProductCreateDto
        {
            categoryId = Guid.NewGuid(),
            Name = "Gaming Mouse",
            Price = 75.00m
        };

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);
        _helperMock.Setup(h => h.GetCategoryOr404(createDto.categoryId)).ReturnsAsync(new Category());
        
        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        Func<Task> action = async () => await _productService.CreateProductAsync(testUser.Id, createDto);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("User seller profile status not approved");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _productRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_WhenDbUpdateExceptionOccurs_DeletesUploadedFileAndRollbacks()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Approved
        );

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);

        var createDto = new ProductCreateDto
        {
            categoryId = Guid.NewGuid(),
            Name = "Gaming Mouse",
            Price = 75.00m,
            Image = fileMock.Object
        };

        var uploadedFileUrl = "uploads/products/mouse.jpg";

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);
        _helperMock.Setup(h => h.GetCategoryOr404(createDto.categoryId)).ReturnsAsync(new Category());
        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        _fileServiceMock.Setup(f => f.UploadFileAsync(createDto.Image))
            .ReturnsAsync(uploadedFileUrl);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("DB connection error"));

        Func<Task> action = async () => await _productService.CreateProductAsync(testUser.Id, createDto);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not save the product to the database.");

        _fileServiceMock.Verify(f => f.DeleteFileAsync(uploadedFileUrl), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenRequestIsValidWithImage_ReturnsTrueAndUpdatesFileAndCache()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Approved
        );
        
        var oldImageUrl = "uploads/products/old-image.jpg";
        var newExpectedImageUrl = "uploads/products/new-image.jpg";

        var testProduct = ProductTestHelper.CreateTestProduct(
            SellerProfileId: testSellerProfile.Id,
            ImageUrl: oldImageUrl
        );

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);

        var updateDto = new ProductUpdateDto
        {
            Name = "Updated Product Name",
            Price = 150.00m,
            Image = fileMock.Object
        };

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        _helperMock.Setup(h => h.GetProductOr404(testProduct.Id))
            .ReturnsAsync(testProduct);

        _fileServiceMock.Setup(f => f.UploadFileAsync(updateDto.Image))
            .ReturnsAsync(newExpectedImageUrl);

        var result = await _productService.UpdateProductAsync(testUser.Id, testProduct.Id, updateDto);

        result.Should().BeTrue();
        testProduct.Name.Should().Be(updateDto.Name);
        testProduct.Price.Should().Be(updateDto.Price!.Value);
        testProduct.ImageUrl.Should().Be(newExpectedImageUrl);

        _fileServiceMock.Verify(f => f.UploadFileAsync(updateDto.Image), Times.Once);
        _fileServiceMock.Verify(f => f.DeleteFileAsync(oldImageUrl), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenProductDoesNotBelongToSeller_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Approved
        );

        var anotherSellerProfileId = Guid.NewGuid();
        var testProduct = ProductTestHelper.CreateTestProduct(
            SellerProfileId: anotherSellerProfileId
        );

        var updateDto = new ProductUpdateDto
        {
            Name = "Hacked Name"
        };

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        _helperMock.Setup(h => h.GetProductOr404(testProduct.Id))
            .ReturnsAsync(testProduct);

        Func<Task> action = async () => await _productService.UpdateProductAsync(testUser.Id, testProduct.Id, updateDto);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Product does not belong to seller");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _productRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenSellerProfileNotApproved_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Pending
        );

        var testProduct = ProductTestHelper.CreateTestProduct(
            SellerProfileId: testSellerProfile.Id
        );

        var updateDto = new ProductUpdateDto
        {
            Name = "Some Name"
        };

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        Func<Task> action = async () => await _productService.UpdateProductAsync(testUser.Id, testProduct.Id, updateDto);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("User seller profile status not approved");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenRequestIsValid_DeletesProductFileAndReturnsTrue()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Approved
        );

        var imageUrl = "uploads/products/item.jpg";

        var testProduct = ProductTestHelper.CreateTestProduct(
            SellerProfileId: testSellerProfile.Id,
            ImageUrl: imageUrl
        );

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        _helperMock.Setup(h => h.GetProductOr404(testProduct.Id))
            .ReturnsAsync(testProduct);

        var result = await _productService.DeleteProductAsync(testUser.Id, testProduct.Id);

        result.Should().BeTrue();

        _fileServiceMock.Verify(f => f.DeleteFileAsync(imageUrl), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _productRepositoryMock.Verify(r => r.DeleteAsync(testProduct), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenProductDoesNotBelongToSeller_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Approved
        );

        var anotherSellerId = Guid.NewGuid();
        var testProduct = ProductTestHelper.CreateTestProduct(
            SellerProfileId: anotherSellerId
        );

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        _helperMock.Setup(h => h.GetProductOr404(testProduct.Id))
            .ReturnsAsync(testProduct);

        Func<Task> action = async () => await _productService.DeleteProductAsync(testUser.Id, testProduct.Id);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Product does not belong to seller");

        _fileServiceMock.Verify(f => f.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _productRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_WhenSellerProfileNotApproved_ThrowsBadRequestException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(
            userId: testUser.Id, 
            status: SellerStatus.Pending
        );

        var testProduct = ProductTestHelper.CreateTestProduct(
            SellerProfileId: testSellerProfile.Id
        );

        _helperMock.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        Func<Task> action = async () => await _productService.DeleteProductAsync(testUser.Id, testProduct.Id);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("User seller profile status not approved");

        _helperMock.Verify(h => h.GetProductOr404(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenDataIsInCache_ReturnsCachedProductWithoutHittingDatabase()
    {
        var productId = Guid.NewGuid();

        var cachedProduct = new ProductResponseDto
        {
            Id = productId,
            Name = "Cached Gaming Mouse",
            Price = 75.00m
        };

        _cacheMock.Setup(c => c.GetDataAsync<ProductResponseDto>($"products:{productId}"))
            .ReturnsAsync(cachedProduct);

        var result = await _productService.GetProductByIdAsync(productId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedProduct);

        _helperMock.Verify(h => h.GetProductOr404(It.IsAny<Guid>()), Times.Never);
        
        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<ProductResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenDataIsNotInCache_FetchesFromDbMapsAndCaches()
    {
        var productId = Guid.NewGuid();

        var testProduct = ProductTestHelper.CreateTestProduct(Id: productId, Name: "DB Gaming Mouse");

        _cacheMock.Setup(c => c.GetDataAsync<ProductResponseDto>($"products:{productId}"))
            .ReturnsAsync((ProductResponseDto?)null);

        _helperMock.Setup(h => h.GetProductOr404(productId))
            .ReturnsAsync(testProduct);

        var result = await _productService.GetProductByIdAsync(productId);

        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be(testProduct.Name);
        result.Price.Should().Be(testProduct.Price);

        _helperMock.Verify(h => h.GetProductOr404(productId), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            $"products:{productId}",
            It.Is<ProductResponseDto>(dto => dto.Id == productId && dto.Name == testProduct.Name),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetProductsAsync_WhenDataIsInCache_ReturnsCachedProductsWithoutHittingRepository()
    {
        var cachedProducts = new List<ProductResponseDto>
        {
            new ProductResponseDto { Id = Guid.NewGuid(), Name = "Cached Product 1", Price = 100.00m },
            new ProductResponseDto { Id = Guid.NewGuid(), Name = "Cached Product 2", Price = 200.00m }
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<ProductResponseDto>>("products:all"))
            .ReturnsAsync(cachedProducts);

        var result = await _productService.GetProductsAsync();

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedProducts);

        _productRepositoryMock.Verify(r => r.GetProductsWithPagination(), Times.Never);
        
        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<List<ProductResponseDto>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetProductsAsync_WhenDataIsNotInCache_FetchesFromRepositoryMapsAndCaches()
    {
        var testProductDtos = new List<ProductResponseDto>
        {
            new ProductResponseDto { Id = Guid.NewGuid(), Name = "DB Product 1", Price = 150.00m },
            new ProductResponseDto { Id = Guid.NewGuid(), Name = "DB Product 2", Price = 250.00m }
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<ProductResponseDto>>("products:all"))
            .ReturnsAsync((List<ProductResponseDto>?)null);

        _productRepositoryMock.Setup(r => r.GetProductsWithPagination())
            .ReturnsAsync(testProductDtos);

        var result = await _productService.GetProductsAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("DB Product 1");
        result[1].Name.Should().Be("DB Product 2");

        _productRepositoryMock.Verify(r => r.GetProductsWithPagination(), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            "products:all",
            It.Is<List<ProductResponseDto>>(list => list.Count == 2),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }
}