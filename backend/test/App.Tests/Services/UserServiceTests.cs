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
using Microsoft.EntityFrameworkCore;


public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IHelperService> _helper;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _helper = new Mock<IHelperService>();
        
        _userService = new UserService(
            _userRepositoryMock.Object,
            _loggerMock.Object,
            _helper.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object,
            _notificationRepositoryMock.Object
        );
    }

    [Fact]
    public async Task GetAllUserNotAdmin_CheckIfUserNotAdmin()
    {
        var testDefaultUsers = new List<User>
        {
            AuthTestHelper.CreateTestUser(roles: UserRole.DefaultUser),
            AuthTestHelper.CreateTestUser(roles: UserRole.DefaultUser),
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<UserResponseDto>>("user:all"))
            .ReturnsAsync((List<UserResponseDto>?)null);

        _userRepositoryMock.Setup(r => r.GetAllUsersNotAdminAsync())
            .ReturnsAsync(testDefaultUsers);

        var result = await _userService.GetAllUsersNotAdmin();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(user => user.Roles.Should().Be(UserRole.DefaultUser));

        _cacheMock.Verify(c => c.SetDataAsync(
            "user:all", 
            It.Is<List<UserResponseDto>>(list => list.Count == 2),
            TimeSpan.FromMinutes(5)
        ), Times.Once);
    }

    [Fact]
    public async Task GetAllUserNotAdmin_ReturnsCachedUsers()
    {
        var cachedUsers = new List<UserResponseDto>
        {
            new() 
            { 
                Id = Guid.NewGuid(), 
                Email = "cacheduser@test.com", 
                UserName = "user",
                Roles = UserRole.DefaultUser 
            }
        };

        _cacheMock.Setup(c => c.GetDataAsync<List<UserResponseDto>>("user:all"))
            .ReturnsAsync(cachedUsers);

        var result = await _userService.GetAllUsersNotAdmin();

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(cachedUsers);

        _userRepositoryMock.Verify(r => r.GetAllUsersNotAdminAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateUserProfile_ReturnOk()
    {
        var testUser = AuthTestHelper.CreateTestUser();

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        var request = new UserUpdateDto
        {
            UserName = "UpdatedUserName",
            Email = "updateuser@example.com"
        };

        var result = await _userService.UpdateUserProfile(testUser.Id, request);

        result.Should().BeTrue();
        
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync("user:all"), Times.Once);
        _cacheMock.Verify(c => c.RemoveDataAsync($"user:{testUser.Id}"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserPasswordAsync_WhenRequestIsValid_ReturnsTrueAndUpdatesPasswordAndCreatesNotification()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var passwordDto = new UserPasswordUpdateDto
        {
            Password = "NewSecurePassword123"
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        var result = await _userService.UpdateUserPasswordAsync(testUser.Id, passwordDto);

        result.Should().BeTrue();

        testUser.Password.Should().NotBe("NewSecurePassword123");

        _notificationRepositoryMock.Verify(n => n.CreateAsync(It.Is<Notification>(notif => 
            notif.UserId == testUser.Id && 
            notif.Title == "Password update" &&
            notif.Type == NotificationType.System
        )), Times.Once);

        _userRepositoryMock.Verify(r => r.UpdateAsync(testUser), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateUserPasswordAsync_WhenDbUpdateExceptionOccurs_RollbacksAndThrowsDatabaseException()
    {
        var testUser = AuthTestHelper.CreateTestUser();
        var passwordDto = new UserPasswordUpdateDto
        {
            Password = "NewSecurePassword123"
        };

        _helper.Setup(h => h.GetUserOr404(testUser.Id))
            .ReturnsAsync(testUser);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Database connection failed"));

        Func<Task> action = async () => await _userService.UpdateUserPasswordAsync(testUser.Id, passwordDto);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not update the user password to the database.");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteUserByAdmin_ReturnOk()
    {
        var testAdminUser = AuthTestHelper.CreateTestUser(roles: UserRole.Admin);
        var testDefaultUser = AuthTestHelper.CreateTestUser(roles: UserRole.DefaultUser);

        _helper.Setup(h => h.GetUserOr404(testAdminUser.Id))
            .ReturnsAsync(testAdminUser);

        _helper.Setup(h => h.GetUserOr404(testDefaultUser.Id))
            .ReturnsAsync(testDefaultUser);

        var result = await _userService.DeleteUserAsync(testAdminUser.Id, testDefaultUser.Id);

        result.Should().BeTrue();

        _userRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync("user:all"), Times.Once);
        _cacheMock.Verify(c => c.RemoveDataAsync($"user:{testDefaultUser.Id}"), Times.Once);
        _cacheMock.Verify(c => c.RemoveDataAsync("analytics:general:all"), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenCurrentUserIdEqualsTargetUserId_ThrowsBadRequestException()
    {
        var adminId = Guid.NewGuid();
        
        var adminUser = new User
        {
            Id = adminId,
            Email = "testadmin@example.com",
            UserName = "testUserName",
            Password = "NewSecurePassword123",
        };

        _helper.Setup(h => h.GetUserOr404(adminId))
            .ReturnsAsync(adminUser);

        Func<Task> action = async () => await _userService.DeleteUserAsync(adminId, adminId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("User can't delete themself");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _userRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ActivateOrDiactivateUser_ReturnsOk()
    {
        var testAdminUser = AuthTestHelper.CreateTestUser(roles: UserRole.Admin);
        var testDefaultUser = AuthTestHelper.CreateTestUser(roles: UserRole.DefaultUser);

        _helper.Setup(h => h.GetUserOr404(testAdminUser.Id))
            .ReturnsAsync(testAdminUser);

        _helper.Setup(h => h.GetUserOr404(testDefaultUser.Id))
            .ReturnsAsync(testDefaultUser);

        var result = await _userService.ActivateOrDiactivateUserAsync(
            testAdminUser.Id,
            testDefaultUser.Id,
            false
        );

        result.Should().BeTrue();

        testDefaultUser.isActive.Should().BeFalse();

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => 
            u.Id == testDefaultUser.Id && u.isActive == false
        )), Times.Once);
        
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync("user:all"), Times.Once);
        _cacheMock.Verify(c => c.RemoveDataAsync($"user:{testDefaultUser.Id}"), Times.Once);
    }
    
    [Fact]
    public async Task AddRoleToUser_ReturnOk()
    {
        var testAdminUser = AuthTestHelper.CreateTestUser(roles: UserRole.Admin);
        var testDefaultUser = AuthTestHelper.CreateTestUser(roles: UserRole.DefaultUser);

        _helper.Setup(h => h.GetUserOr404(testAdminUser.Id))
            .ReturnsAsync(testAdminUser);

        _helper.Setup(h => h.GetUserOr404(testDefaultUser.Id))
            .ReturnsAsync(testDefaultUser);

        var result = await _userService.AddRoleToUserAsync(
            testAdminUser.Id,
            testDefaultUser.Id,
            UserRole.Moderator
        );

        testDefaultUser.Roles.Should().HaveFlag(UserRole.Moderator);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => 
            u.Id == testDefaultUser.Id && u.Roles.HasFlag(UserRole.Moderator)
        )), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync("user:all"), Times.Once);
        _cacheMock.Verify(c => c.RemoveDataAsync($"user:{testDefaultUser.Id}"), Times.Once);
    }

    [Fact]
    public async Task AddRoleToUser_IfUserAlreadyHaveRole_ThrowsBadRequestException()
    {
        var testAdminUser = AuthTestHelper.CreateTestUser(roles: UserRole.Admin);
        var testDefaultUser = AuthTestHelper.CreateTestUser(roles: UserRole.DefaultUser | UserRole.Moderator);

        _helper.Setup(h => h.GetUserOr404(testAdminUser.Id))
            .ReturnsAsync(testAdminUser);

        _helper.Setup(h => h.GetUserOr404(testDefaultUser.Id))
            .ReturnsAsync(testDefaultUser);

        Func<Task> action = async () => await _userService.AddRoleToUserAsync(
            testAdminUser.Id,
            testDefaultUser.Id,
            UserRole.Moderator
        );

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("User already have this role");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RemoveRoleFromUser_WhenRoleExists_RemovesRoleAndReturnsTrue()
    {
        var testAdminUser = AuthTestHelper.CreateTestUser(roles: UserRole.Admin);
        var testDefaultUser = AuthTestHelper.CreateTestUser(roles: UserRole.DefaultUser | UserRole.Moderator);

        _helper.Setup(h => h.GetUserOr404(testAdminUser.Id))
            .ReturnsAsync(testAdminUser);

        _helper.Setup(h => h.GetUserOr404(testDefaultUser.Id))
            .ReturnsAsync(testDefaultUser);

        var result = await _userService.RemoveRoleFromUserAsync(
            testAdminUser.Id,
            testDefaultUser.Id,
            UserRole.Moderator
        );

        result.Should().BeTrue();

        testDefaultUser.Roles.HasFlag(UserRole.Moderator).Should().BeFalse();
        testDefaultUser.Roles.HasFlag(UserRole.DefaultUser).Should().BeTrue();

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => 
            u.Id == testDefaultUser.Id && !u.Roles.HasFlag(UserRole.Moderator)
        )), Times.Once);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleFromUser_IfHaveOneRoleNotPossibleToDelete_ThrowsBadRequestException()
    {
        var testAdminUser = AuthTestHelper.CreateTestUser(roles: UserRole.Admin);
        var testDefaultUser = AuthTestHelper.CreateTestUser(roles: UserRole.DefaultUser);

        _helper.Setup(h => h.GetUserOr404(testAdminUser.Id))
            .ReturnsAsync(testAdminUser);

        _helper.Setup(h => h.GetUserOr404(testDefaultUser.Id))
            .ReturnsAsync(testDefaultUser);

        Func<Task> action = async () => await _userService.RemoveRoleFromUserAsync(
            testAdminUser.Id,
            testDefaultUser.Id,
            UserRole.DefaultUser
        );

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("User must have at least one role");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }
}