using App.DTOs;
using App.Exceptions;
using App.Repositories;

namespace App.Services;


public interface IUserService
{
    Task<UserProfileResponseDto> GetUserProfile(Guid userId);
    Task<List<UserResponseDto>> GetAllUsersNotAdmin();
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<UserProfileResponseDto> GetUserProfile(Guid userId)
    {
        var user = await _repository.GetByIdAsync(userId);

        if (user is null)
        {
            _logger.LogWarning("User not found by id: {userId}", userId);
            throw new NotFoundException("User not found");
        }

        _logger.LogInformation("Successful response of user profile: {userId}", userId);

        return new UserProfileResponseDto
        {
            Email = user.Email,
            Username = user.UserName,
            isActive = user.isActive
        };
    }

    public async Task<List<UserResponseDto>> GetAllUsersNotAdmin()
    {
        var users = await _repository.GetAllUsersNotAdminAsync();

        _logger.LogInformation("Successful response of users");

        return users.Select(user => new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = user.Roles,
            isActive = user.isActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        }).ToList();
    }
}