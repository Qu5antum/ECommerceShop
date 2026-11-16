using Moq;
using Microsoft.Extensions.Logging;

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;
using App.Controllers;
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
}