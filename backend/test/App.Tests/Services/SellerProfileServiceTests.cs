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
using Microsoft.EntityFrameworkCore;


public class SellerProfileServiceTests
{
    private readonly Mock<ISellerProfileRepository> _sellerProfileRepositoryMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<IFileStorageService> _fileServiceMock;
    private readonly Mock<ILogger<SellerProfileService>> _loggerMock;
    private readonly Mock<IHelperService> _helper;
    private readonly SellerProfileService _sellerProfileService;

    public SellerProfileServiceTests()
    {
        _sellerProfileRepositoryMock = new Mock<ISellerProfileRepository>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _loggerMock = new Mock<ILogger<SellerProfileService>>();
        _fileServiceMock = new Mock<IFileStorageService>();
        _helper = new Mock<IHelperService>();
        
        _sellerProfileService = new SellerProfileService(
            _sellerProfileRepositoryMock.Object,
            _notificationRepositoryMock.Object,
            _loggerMock.Object,
            _helper.Object,
            _fileServiceMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task CreateSellerProfileAsync_WhenRequestIsValidWithImage_ReturnsCreatedProfileAndUploadsImage()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024); 

        var sellerCreateDto = new SellerProfileCreateDto
        {
            StoreName = "Tech Store",
            Description = "Best electronics",
            Image = fileMock.Object
        };

        var expectedFileUrl = "uploads/stores/tech-store.jpg";

        _helper.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);
        
        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync((SellerProfile?)null);

        _sellerProfileRepositoryMock.Setup(r => r.IsStoreNameTakenAsync(sellerCreateDto.StoreName))
            .ReturnsAsync(false);

        _fileServiceMock.Setup(f => f.UploadFileAsync(sellerCreateDto.Image))
            .ReturnsAsync(expectedFileUrl);

        var result = await _sellerProfileService.CreateSellerProfileAsync(testUser.Id, sellerCreateDto);

        result.Should().NotBeNull();
        result.StoreName.Should().Be(sellerCreateDto.StoreName);

        _fileServiceMock.Verify(f => f.UploadFileAsync(sellerCreateDto.Image), Times.Once);

        _sellerProfileRepositoryMock.Verify(r => r.CreateAsync(It.Is<SellerProfile>(p => 
            p.StoreName == sellerCreateDto.StoreName && 
            p.ImageUrl == expectedFileUrl
        )), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateSellerProfileAsync_WhenDbUpdateExceptionOccurs_DeletesUploadedFileAndRollbacks()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);

        var sellerCreateDto = new SellerProfileCreateDto
        {
            StoreName = "Tech Store",
            Description = "Best electronics",
            Image = fileMock.Object
        };

        var uploadedFileUrl = "uploads/stores/tech-store.jpg";

        _helper.Setup(h => h.GetUserOr404(testUser.Id)).ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync((SellerProfile?)null);

        _sellerProfileRepositoryMock.Setup(r => r.IsStoreNameTakenAsync(sellerCreateDto.StoreName))
            .ReturnsAsync(false);

        _fileServiceMock.Setup(f => f.UploadFileAsync(sellerCreateDto.Image))
            .ReturnsAsync(uploadedFileUrl);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Database error"));

        Func<Task> action = async () => await _sellerProfileService.CreateSellerProfileAsync(testUser.Id, sellerCreateDto);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not save the seller profile to the database.");

        _fileServiceMock.Verify(f => f.DeleteFileAsync(uploadedFileUrl), Times.Once);

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateSellerProfileAsync_WithImageUpdate_ReturnsOk()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile();

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024); 
    
        var testUpdateSellerProfile = new SellerProfileUpdateDto
        {
            StoreName = "TestUpdateStoreName",
            Image = fileMock.Object
        };
        
        var oldImageUrl = "uploads/stores/default-test.jpg";
        var expectedFileUrl = "uploads/stores/tech-store1.jpg";

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetByIdAsync(testSellerProfile.Id))
            .ReturnsAsync(testSellerProfile);

        _fileServiceMock.Setup(f => f.UploadFileAsync(testUpdateSellerProfile.Image))
            .ReturnsAsync(expectedFileUrl);

        var result = await _sellerProfileService.UpdateSellerProfileAsync(
            testUser.Id,
            testSellerProfile.Id,
            testUpdateSellerProfile
        );

        result.Should().BeTrue();

        _sellerProfileRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SellerProfile>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _fileServiceMock.Verify(f => f.DeleteFileAsync(oldImageUrl), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync($"seller:{testSellerProfile.Id}"));
    }

    [Fact]
    public async Task UpdateSellerProfile_IfSellerProfileNotFound_ThrowsNotFoundException()
    {
        var nonExistentProfileId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser();

        var updateDto = new SellerProfileUpdateDto
        {
            StoreName = "New Name"
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _sellerProfileRepositoryMock.Setup(r => r.GetByIdAsync(nonExistentProfileId))
            .ReturnsAsync((SellerProfile?)null);

        Func<Task> action = async () => await _sellerProfileService.UpdateSellerProfileAsync(
            testUser.Id, 
            nonExistentProfileId, 
            updateDto
        );

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Seller profile not found");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _sellerProfileRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SellerProfile>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_ReturnsProfile()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(userId: testUser.Id);

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);
        
        _cacheMock.Setup(c => c.GetDataAsync<SellerProfileResponseDto>($"seller:{testUser.Id}"))
            .ReturnsAsync((SellerProfileResponseDto?)null);

        _sellerProfileRepositoryMock.Setup(r => r.GetSellerProfileByUserIdAsync(testUser.Id))
            .ReturnsAsync(testSellerProfile);

        var result = await _sellerProfileService.GetCurrentUserProfileAsync(testUser.Id);

        result.Should().NotBeNull();

        _cacheMock.Verify(c => c.SetDataAsync(
            $"seller:{testUser.Id}",
            result,
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_TestOfCachedDataOfProfile()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile(userId: testUser.Id);

        var cachedTestSellerProfile = new SellerProfileResponseDto
        {
            Id = testSellerProfile.Id,
            userId = testSellerProfile.userId,
            StoreName = testSellerProfile.StoreName,
            Description = testSellerProfile.Description,
            ImageUrl = testSellerProfile.ImageUrl,
            Status = testSellerProfile.Status,
            CreatedAt = testSellerProfile.CreatedAt,
            UpdatedAt = testSellerProfile.UpdatedAt
        };

        _cacheMock.Setup(c => c.GetDataAsync<SellerProfileResponseDto>($"seller:{testUser.Id}"))
            .ReturnsAsync(cachedTestSellerProfile);

        var result = await _sellerProfileService.GetCurrentUserProfileAsync(testUser.Id);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedTestSellerProfile);

        _sellerProfileRepositoryMock.Verify(r => r.GetSellerProfileByUserIdAsync(testUser.Id), Times.Never);
    }

    [Fact]
    public async Task GetUserSellerProfile_NotCachedData_Returns()
    {
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile();

        _cacheMock.Setup(c => c.GetDataAsync<SellerProfileResponseDto>($"seller:{testSellerProfile.Id}"))
            .ReturnsAsync((SellerProfileResponseDto?)null);

        _helper.Setup(h => h.GetSellerProfileOr404(testSellerProfile.Id))
            .ReturnsAsync(testSellerProfile);

        var result = await _sellerProfileService.GetUserSellerProfileAsync(testSellerProfile.Id);

        result.Should().NotBeNull();
        
        _cacheMock.Verify(c => c.SetDataAsync(
            $"seller:{testSellerProfile.Id}",
            result,
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetUserSellerProfile_ReturnsCachedData()
    {
        var testSellerProfile = SellerProfileTestHelper.CreateTestSellerProfile();

        var cachedTestSellerProfile = new SellerProfileResponseDto
        {
            Id = testSellerProfile.Id,
            userId = testSellerProfile.userId,
            StoreName = testSellerProfile.StoreName,
            Description = testSellerProfile.Description,
            ImageUrl = testSellerProfile.ImageUrl,
            Status = testSellerProfile.Status,
            CreatedAt = testSellerProfile.CreatedAt,
            UpdatedAt = testSellerProfile.UpdatedAt
        };
        
        _cacheMock.Setup(c => c.GetDataAsync<SellerProfileResponseDto>($"seller:{testSellerProfile.Id}"))
            .ReturnsAsync(cachedTestSellerProfile);
        
        var result = await _sellerProfileService.GetUserSellerProfileAsync(testSellerProfile.Id);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedTestSellerProfile);

        _helper.Verify(h => h.GetSellerProfileOr404(testSellerProfile.Id), Times.Never);
    }

    [Fact]
public async Task GetSellerProfileImageAsync_WhenImageExists_ReturnsFileStreamAndContentType()
{
    var sellerId = Guid.NewGuid();
    var expectedImageUrl = "uploads/stores/logo.jpg";
    var expectedContentType = "image/jpeg";
    
    Stream expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });

    var testSeller = SellerProfileTestHelper.CreateTestSellerProfile(
        Id: sellerId,
        imageUrl: expectedImageUrl
    );

    _helper.Setup(h => h.GetSellerProfileOr404(sellerId))
        .ReturnsAsync(testSeller);

    _fileServiceMock.Setup(f => f.GetFileAsync(expectedImageUrl))
        .ReturnsAsync((expectedStream, expectedContentType));

    var result = await _sellerProfileService.GetSellerProfileImageAsync(sellerId);

    result.Should().NotBeNull();
    result.Value.ContentType.Should().Be(expectedContentType);

    _fileServiceMock.Verify(f => f.GetFileAsync(expectedImageUrl), Times.Once);
}

[Fact]
public async Task GetSellerProfileImageAsync_WhenImageUrlIsNull_ReturnsNull()
{
    var sellerId = Guid.NewGuid();

    var testSeller = SellerProfileTestHelper.CreateTestSellerProfile(
        Id: sellerId,
        imageUrl: null! 
    );

    _helper.Setup(h => h.GetSellerProfileOr404(sellerId))
        .ReturnsAsync(testSeller);

    var result = await _sellerProfileService.GetSellerProfileImageAsync(sellerId);

    result.Should().BeNull();

    _fileServiceMock.Verify(f => f.GetFileAsync(It.IsAny<string>()), Times.Never);
}

[Fact]
public async Task GetSellerProfileImageAsync_WhenSellerNotFound_ThrowsNotFoundException()
{
    var nonExistentSellerId = Guid.NewGuid();

    _helper.Setup(h => h.GetSellerProfileOr404(nonExistentSellerId))
        .ThrowsAsync(new NotFoundException("Seller profile not found"));

    Func<Task> action = async () => await _sellerProfileService.GetSellerProfileImageAsync(nonExistentSellerId);

    await action.Should().ThrowAsync<NotFoundException>()
        .WithMessage("Seller profile not found");

    _fileServiceMock.Verify(f => f.GetFileAsync(It.IsAny<string>()), Times.Never);
}
}