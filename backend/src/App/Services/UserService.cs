using App.DTOs;
using App.Repositories;

namespace App.Services;


public interface IUserService
{
    Task<UserProfileResponseDto> GetUserProfile(Guid userId);
    Task<List<UserResponseDto>> GetAllUsersNotAdmin();
    Task<bool> UpdateUserProfile(Guid userId, UserUpdateDto userUpdateDto);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;
    private readonly IHelperService _helper;

    public UserService(IUserRepository repository, ILogger<UserService> logger, IHelperService helper)
    {
        _repository = repository;
        _logger = logger;
        _helper = helper;
    }

    public async Task<UserProfileResponseDto> GetUserProfile(Guid userId)
    {
        var user = await _helper.GetUserOr404(userId);

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

    public async Task<bool> UpdateUserProfile(Guid userId, UserUpdateDto userUpdateDto)
    {
        var user = await _helper.GetUserOr404(userId);

        if (userUpdateDto.UserName != null)
        {
            user.UserName = userUpdateDto.UserName;
        }
        if (userUpdateDto.Email != null)
        {
            user.Email = userUpdateDto.Email;
        }
        if (userUpdateDto.Password != null)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(userUpdateDto.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user);

        _logger.LogInformation("User successfully updated");

        return true;
    }
}