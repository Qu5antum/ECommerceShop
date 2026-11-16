using Moq;
using Microsoft.Extensions.Logging;

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;
using App.Enum;


public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly Mock<IHelperService> _helper;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _helper = new Mock<IHelperService>();
        _loggerMock = new Mock<ILogger<NotificationService>>();
        
        _notificationService = new NotificationService(
            _notificationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _loggerMock.Object,
            _helper.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task GetNotifications_WhenDataNotCached_FetchesFromRepository()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testNotifications = new List<Notification>
        {
            new Notification { Id = Guid.NewGuid(), UserId = testUser.Id, Title = "TestTitle1", Message = "TestMessage1", IsRead = false },
            new Notification { Id = Guid.NewGuid(), UserId = testUser.Id, Title = "TestTitle2", Message = "TestMessage2", IsRead = false }
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<List<NotificationResponseDto>>($"notifications:{testUser.Id}:isRead:{false}"))
            .ReturnsAsync((List<NotificationResponseDto>?)null);

        _notificationRepositoryMock.Setup(r => r.GetNotificationsAsync(testUser.Id, false))
            .ReturnsAsync(testNotifications);
            
        var result = await _notificationService.GetNotificationsAsync(testUser.Id, false);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("TestTitle1");
        result[1].Title.Should().Be("TestTitle2");

        _notificationRepositoryMock.Verify(r => r.GetNotificationsAsync(testUser.Id, false), Times.Once);
        
        _cacheMock.Verify(c => c.SetDataAsync(
            $"notifications:{testUser.Id}:isRead:{false}",
            It.Is<List<NotificationResponseDto>>(list => list.Count == 2),
            TimeSpan.FromMinutes(1)
        ), Times.Once);
    }

    [Fact]
    public async Task GetNotifications_WhenDatainCache_FetchesFromCache()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        var testNotifications = new List<NotificationResponseDto>
        {
            new NotificationResponseDto { Id = Guid.NewGuid(), UserId = testUser.Id, Title = "TestTitle1", Message = "TestMessage1", IsRead = false },
            new NotificationResponseDto { Id = Guid.NewGuid(), UserId = testUser.Id, Title = "TestTitle2", Message = "TestMessage2", IsRead = false }
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<List<NotificationResponseDto>>($"notifications:{testUser.Id}:isRead:{false}"))
            .ReturnsAsync(testNotifications);

        var result = await _notificationService.GetNotificationsAsync(testUser.Id, false);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("TestTitle1");
        result[1].Title.Should().Be("TestTitle2");

        _notificationRepositoryMock.Verify(r => r.GetNotificationsAsync(testUser.Id, false), Times.Never);
        
        _cacheMock.Verify(c => c.SetDataAsync(
            It.IsAny<string>(),
            It.Is<List<NotificationResponseDto>>(list => list.Count == 2),
            It.IsAny<TimeSpan>()
        ), Times.Never);
    }

    [Fact]
    public async Task GetUserNotificationAsync_WhenDataIsInCache_ReturnsCachedNotificationWithoutHittingHelper()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var cacheKey = $"notification:user:{userId}:{notificationId}"; // Формат вашего GetNotificationUserCacheKey

        var cachedDto = new NotificationResponseDto
        {
            Id = notificationId,
            UserId = userId,
            Title = "Cached Title",
            Message = "Cached Message"
        };

        // 1. Имитируем Cache Hit
        _cacheMock.Setup(c => c.GetDataAsync<NotificationResponseDto>($"notification:user:{userId}:{notificationId}"))
            .ReturnsAsync(cachedDto);

        // Act
        var result = await _notificationService.GetUserNotificationAsync(userId, notificationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedDto);

        // Так как данные взяты из кэша первым делом, хелпер не должен вызываться
        _helper.Verify(h => h.GetNotificationOr404(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetUserNotificationAsync_WhenCacheMissAndNotificationDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        var testNotification = new Notification
        {
            Id = notificationId,
            UserId = otherUserId, 
            Title = "Secret Title",
            Message = "Secret Message"
        };

        _cacheMock.Setup(c => c.GetDataAsync<NotificationResponseDto>($"notification:user:{userId}:{notificationId}"))
            .ReturnsAsync((NotificationResponseDto?)null);

        _helper.Setup(h => h.GetNotificationOr404(notificationId))
            .ReturnsAsync(testNotification);

        Func<Task> action = async () => await _notificationService.GetUserNotificationAsync(userId, notificationId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Notification not belong to user");

        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<NotificationResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetUserNotificationAsync_WhenCacheMissAndValidUser_MapsAndCachesNotification()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        var testNotification = new Notification
        {
            Id = notificationId,
            UserId = userId, 
            Title = "Db Title",
            Message = "Db Message",
            Type = NotificationType.System,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.GetDataAsync<NotificationResponseDto>($"notification:user:{userId}:{notificationId}"))
            .ReturnsAsync((NotificationResponseDto?)null);

        _helper.Setup(h => h.GetNotificationOr404(notificationId))
            .ReturnsAsync(testNotification);

        var result = await _notificationService.GetUserNotificationAsync(userId, notificationId);

        result.Should().NotBeNull();
        result.Id.Should().Be(notificationId);
        result.UserId.Should().Be(userId);
        result.Title.Should().Be("Db Title");

        _cacheMock.Verify(c => c.SetDataAsync(
            $"notification:user:{userId}:{notificationId}",
            It.Is<NotificationResponseDto>(dto => dto.Id == notificationId && dto.Title == "Db Title"),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetCountOfUnreadNotification_WhenNotCachedData_FetchedFromCache()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<int?>($"notifications:{testUser.Id}:unread-count"))
            .ReturnsAsync((int?)null);
        
        _notificationRepositoryMock.Setup(r => r.GetCountOfUnReadNotifications(testUser.Id))
            .ReturnsAsync(2);

        var result = await _notificationService.GetCountOfUnreadNotificationsAsync(testUser.Id);
        
        result.Should().Be(2);

        _cacheMock.Verify(c => c.SetDataAsync(
            $"notifications:{testUser.Id}:unread-count",
            2,
            TimeSpan.FromMinutes(1)
        ), Times.Once);
    }

    [Fact]
    public async Task GetCountOfUnreadNotification_WhenCachedData_FetchesFromCache()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _cacheMock.Setup(c => c.GetDataAsync<int?>($"notifications:{testUser.Id}:unread-count"))
            .ReturnsAsync(2);

        var result = await _notificationService.GetCountOfUnreadNotificationsAsync(testUser.Id);
        
        result.Should().Be(2);

        _notificationRepositoryMock.Verify(r => r.GetCountOfUnReadNotifications(testUser.Id), Times.Never);

        _cacheMock.Verify(c => c.SetDataAsync(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<TimeSpan>()
        ), Times.Never);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenUserExists_MarksNotificationsAsReadAndInvalidatesCache()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        await _notificationService.MarkAllAsReadAsync(testUser.Id);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _notificationRepositoryMock.Verify(r => r.MarkAllAsReadAsync(testUser.Id), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenUserNotFound_ThrowsNotFoundExceptionAndDoesNotCommit()
    {
        var nonExistentUserId = Guid.NewGuid();

        _helper.Setup(h => h.GetUserOr404(nonExistentUserId))
            .ThrowsAsync(new NotFoundException("User not found"));

        Func<Task> action = async () => await _notificationService.MarkAllAsReadAsync(nonExistentUserId);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found");

        _notificationRepositoryMock.Verify(r => r.MarkAllAsReadAsync(It.IsAny<Guid>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendNotificationsAdmin_ReturnsOk()
    {
        var testUserIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        var testCreateNotificationDto = new CreateNotificationDto
        {
            Title = "TestAdminTitle", Message = "TestAdminMessage"
        };

        _userRepositoryMock.Setup(r => r.GetUserIdsWithDefaultRolesAsync())
            .ReturnsAsync(testUserIds);

        var result = await _notificationService.SendNotificationsAdminAsync(testCreateNotificationDto);

        result.Should().BeTrue();

        _notificationRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<List<Notification>>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync("notifications:all"), Times.Once);
    }

    [Fact]
    public async Task GetAllNotificationsAdminAsync_WhenDataIsInCache_ReturnsCachedNotificationsWithoutHittingRepository()
    {
        var notificationType = NotificationType.System;

        var cachedNotifications = new List<NotificationResponseDto>
        {
            new NotificationResponseDto 
            { 
                Id = Guid.NewGuid(), 
                UserId = Guid.NewGuid(), 
                Title = "Admin Alert Cached", 
                Message = "Cached message",
                Type = NotificationType.System,
                IsRead = false 
            }
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<NotificationResponseDto>>("notifications:all"))
            .ReturnsAsync(cachedNotifications);

        var result = await _notificationService.GetAllNotificationsAdminAsync(notificationType);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedNotifications);

        _notificationRepositoryMock.Verify(r => r.GetAllNotificationWithPagination(
            It.IsAny<NotificationType?>(), 
            It.IsAny<int>(), 
            It.IsAny<int>()
        ), Times.Never);
    }

    [Fact]
    public async Task GetAllNotificationsAdminAsync_WhenDataIsNotInCache_FetchesFromRepositoryWithPaginationAndCaches()
    {
        var notificationType = NotificationType.System;

        var dbNotifications = new List<Notification>
        {
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "System Maintenance",
                Message = "Scheduled maintenance tonight",
                Type = NotificationType.System,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Security Update",
                Message = "New policy applied",
                Type = NotificationType.System,
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<NotificationResponseDto>>("notifications:all"))
            .ReturnsAsync((List<NotificationResponseDto>?)null);

        _notificationRepositoryMock.Setup(r => r.GetAllNotificationWithPagination(notificationType))
            .ReturnsAsync(dbNotifications);

        var result = await _notificationService.GetAllNotificationsAdminAsync(notificationType);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("System Maintenance");
        result[1].Title.Should().Be("Security Update");

        _notificationRepositoryMock.Verify(r => r.GetAllNotificationWithPagination(notificationType), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            "notifications:all",
            It.Is<List<NotificationResponseDto>>(list => list.Count == 2),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetNotificationAdminAsync_WhenDataIsInCache_ReturnsCachedNotificationWithoutCallingHelper()
    {
        var notificationId = Guid.NewGuid();

        var cachedDto = new NotificationResponseDto
        {
            Id = notificationId,
            UserId = Guid.NewGuid(),
            Title = "Admin System Alert",
            Message = "System warning"
        };

        _cacheMock.Setup(c => c.GetDataAsync<NotificationResponseDto>($"notifications:{notificationId}"))
            .ReturnsAsync(cachedDto);

        var result = await _notificationService.GetNotificationAdminAsync(notificationId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedDto);

        _helper.Verify(h => h.GetNotificationOr404(It.IsAny<Guid>()), Times.Never);
        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<NotificationResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetNotificationAdminAsync_WhenCacheMiss_FetchesFromHelperMapsAndCaches()
    {
        var notificationId = Guid.NewGuid();

        var testNotification = new Notification
        {
            Id = notificationId,
            UserId = Guid.NewGuid(),
            Title = "Security Notice",
            Message = "Policy update applied",
            Type = NotificationType.System,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _cacheMock.Setup(c => c.GetDataAsync<NotificationResponseDto>($"notifications:{notificationId}"))
            .ReturnsAsync((NotificationResponseDto?)null);

        _helper.Setup(h => h.GetNotificationOr404(notificationId))
            .ReturnsAsync(testNotification);

        var result = await _notificationService.GetNotificationAdminAsync(notificationId);

        result.Should().NotBeNull();
        result.Id.Should().Be(notificationId);
        result.UserId.Should().Be(testNotification.UserId);
        result.Title.Should().Be("Security Notice");
        result.Message.Should().Be("Policy update applied");

        _helper.Verify(h => h.GetNotificationOr404(notificationId), Times.Once);

        _cacheMock.Verify(c => c.SetDataAsync(
            $"notifications:{notificationId}",
            It.Is<NotificationResponseDto>(dto => dto.Id == notificationId && dto.Title == "Security Notice"),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetNotificationAdminAsync_WhenCacheMissAndNotFoundInDb_ThrowsNotFoundException()
    {
        var notificationId = Guid.NewGuid();

        _cacheMock.Setup(c => c.GetDataAsync<NotificationResponseDto>($"notifications:{notificationId}"))
            .ReturnsAsync((NotificationResponseDto?)null);

        _helper.Setup(h => h.GetNotificationOr404(notificationId))
            .ThrowsAsync(new NotFoundException("Notification not found"));

        Func<Task> action = async () => await _notificationService.GetNotificationAdminAsync(notificationId);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Notification not found");

        _cacheMock.Verify(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<NotificationResponseDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task DeleteNotificationAdminAsync_WhenNotificationExists_DeletesItemCommitsTransactionAndInvalidatesCache()
    {
        var notificationId = Guid.NewGuid();

        var testNotification = new Notification
        {
            Id = notificationId,
            UserId = Guid.NewGuid(),
            Title = "Test Notification",
            Message = "Test Content"
        };

        _helper.Setup(h => h.GetNotificationOr404(notificationId))
            .ReturnsAsync(testNotification);

        var result = await _notificationService.DeleteNotificationAdminAsync(notificationId);

        result.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _notificationRepositoryMock.Verify(r => r.DeleteAsync(testNotification), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task DeleteNotificationAdminAsync_WhenNotificationNotFound_ThrowsNotFoundException()
    {
        var notificationId = Guid.NewGuid();

        _helper.Setup(h => h.GetNotificationOr404(notificationId))
            .ThrowsAsync(new NotFoundException("Notification not found"));

        Func<Task> action = async () => await _notificationService.DeleteNotificationAdminAsync(notificationId);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Notification not found");

        _notificationRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Notification>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }
}