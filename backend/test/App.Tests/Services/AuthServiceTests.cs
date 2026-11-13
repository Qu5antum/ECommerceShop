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

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _authRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _authRepositoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task RegisterUser_WhenEmailAndUsernameAreUnique_ReturnsTrueAndCreatesUser()
    {
        var request = new RegisterRequest
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "SecurePassword123"
        };

        _authRepositoryMock.Setup(r => r.GetUserByEmail(request.Email))
            .ReturnsAsync((User?)null);

        _authRepositoryMock.Setup(r => r.GetUserByUserName(request.UserName))
            .ReturnsAsync((User?)null);

        var result = await _authService.RegisterUser(request);

        result.Should().BeTrue();

        _authRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_WhenEmailAlreadyExists_ThrowsAlreadyExistsException()
    {
        var request = new RegisterRequest
        {
            UserName = "testuser",
            Email = "existing@example.com",
            Password = "SecurePassword123"
        };

        _authRepositoryMock.Setup(r => r.GetUserByEmail(request.Email))
            .ReturnsAsync(new User { Email = request.Email });

        Func<Task> action = async () => await _authService.RegisterUser(request);

        await action.Should().ThrowAsync<AlreadyExistsException>();

        _authRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
        
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task LoginUser_WhenCredentialsAreValid_ReturnsLoginResponseWithToken()
    {
        var password = "SecurePassword123";
        
        var existingUser = AuthTestHelper.CreateTestAdminUser(
            email: "test@example.com", 
            password: password
        );

        _authRepositoryMock.Setup(r => r.GetUserByEmail(existingUser.Email))
            .ReturnsAsync(existingUser);

        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("ThisIsAVeryLongSecretKeyForTestingPurposes12345!");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        var request = new LoginRequest
        {
            Email = existingUser.Email,
            Password = password
        };

        var result = await _authService.LoginUser(request);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginUser_WhenUserIsInactive_ThrowsUnauthorizedException()
    {
        var inactiveUser = AuthTestHelper.CreateTestAdminUser(
            email: "inactive@example.com", 
            password: "Password123", 
            isActive: false
        );

        _authRepositoryMock.Setup(r => r.GetUserByEmail(inactiveUser.Email))
            .ReturnsAsync(inactiveUser);

        var request = new LoginRequest
        {
            Email = inactiveUser.Email,
            Password = "Password123"
        };

        Func<Task> action = async () => await _authService.LoginUser(request);

        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("User account is inactive");
    }

    [Fact]
    public async Task LoginUser_WhenEmailDoesNotExist_ThrowsUnauthorizedException()
    {
        var request = new LoginRequest
        {
            Email = "wrong_email@example.com", 
            Password = "Password123"
        };

        _authRepositoryMock.Setup(r => r.GetUserByEmail(request.Email))
            .ReturnsAsync((User?)null);

        Func<Task> action = async () => await _authService.LoginUser(request);

        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password");
    }

    [Fact]
    public async Task LoginUser_WhenEmailExistsButWrongPassword_ThrowsUnauthorizedException()
    {
        var wrongPasswordUser = AuthTestHelper.CreateTestAdminUser(
            email: "test@example.com", 
            password: "Password123"
        );

        _authRepositoryMock.Setup(r => r.GetUserByEmail(wrongPasswordUser.Email))
            .ReturnsAsync(wrongPasswordUser);

        var request = new LoginRequest
        {
            Email = wrongPasswordUser.Email,
            Password = "WrongPassword123"
        };

        Func<Task> action = async () => await _authService.LoginUser(request);

        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password");
    }
}