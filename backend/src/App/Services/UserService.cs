using App.DTOs;
using App.Enum;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Services.Caching;
using App.Transactions;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface IUserService
{
    Task<UserResponseDto> GetUserById(Guid userId);
    Task<List<UserResponseDto>> GetAllUsersNotAdmin();
    Task<bool> UpdateUserProfile(Guid userId, UserUpdateDto userUpdateDto);
    Task<bool> DeleteUserAsync(Guid currentUserId, Guid userId);
    Task<bool> ActivateOrDiactivateUserAsync(Guid currentUserId, Guid userId, bool isActive);
    Task<bool> AddRoleToUserAsync(Guid currentUserId, Guid userId, UserRole role);
    Task<bool> RemoveRoleFromUserAsync(Guid currentUserId, Guid userId, UserRole role);
    Task<bool> UpdateUserPasswordAsync(Guid userId, UserPasswordUpdateDto passwordUpdateDto);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCacheService _cache;

    private static string GetUserCacheKeyById(Guid userId)
    {
        return $"user:{userId}";
    }

    private static string GetUsersCachedKey()
    {
        return $"user:all";
    }

    private static string GetGeneralAnalyticsCacheKey()
    {
        return $"analytics:general:all";
    }

    public UserService(
        IUserRepository repository, 
        ILogger<UserService> logger, 
        IHelperService helper, 
        IUnitOfWork unitOfWork, 
        IRedisCacheService cache,
        INotificationRepository notificationRepository)
    {
        _repository = repository;
        _notificationRepository = notificationRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UserResponseDto> GetUserById(Guid userId)
    {
        var user = await _helper.GetUserOr404(userId);

        var cacheKey = GetUserCacheKeyById(userId);

        var cachedUser = await _cache.GetDataAsync<UserResponseDto>(cacheKey);

        if (cachedUser is not null)
        {
            _logger.LogInformation("User retrieved from redis cache: {userId}", userId);
            return cachedUser;
        }

        var result = new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = user.Roles,
            isActive = user.isActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("Successful response of user profile: {userId}", userId);

        return result;
    }

    public async Task<List<UserResponseDto>> GetAllUsersNotAdmin()
    {
        var cacheKey = GetUsersCachedKey();

        var cachedUsers = await _cache.GetDataAsync<List<UserResponseDto>>(cacheKey);

        if (cachedUsers is not null)
        {
            _logger.LogInformation("Users retrieved from redis cache");
            return cachedUsers;
        }

        var users = await _repository.GetAllUsersNotAdminAsync();

        var result = users.Select(user => new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = user.Roles,
            isActive = user.isActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        }).ToList();

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("Successful response of users");

        return result;
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

        try
        {
            user.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("User successfully updated: {userId}", userId);
            
            await _cache.RemoveDataAsync(GetUsersCachedKey());
            await _cache.RemoveDataAsync(GetUserCacheKeyById(userId));

            _logger.LogInformation("User deleted from redis cache: {userId}", userId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while updating the user: {Message}", ex.Message);
            throw new DatabaseException("Could not update the user profile to the database.");
        }
    }
    
    public async Task<bool> UpdateUserPasswordAsync(Guid userId, UserPasswordUpdateDto passwordUpdateDto)
    {
        var user = await _helper.GetUserOr404(userId);
        await _unitOfWork.BeginTransactionAsync();

        if (passwordUpdateDto.Password != null)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(passwordUpdateDto.Password);
        }

        try
        {
            user.UpdatedAt = DateTime.UtcNow;

            var newNotification = new Notification
            {
                UserId = userId,
                Title = "Password update",
                Message = "Your password successfully updated",
                Type = NotificationType.System
            };

            await _notificationRepository.CreateAsync(newNotification);

            await _repository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Password successfully updated: {userId}", userId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while updating password of user: {Message}", ex.Message);
            throw new DatabaseException("Could not update the user password to the database.");
        }
    }

    public async Task<bool> DeleteUserAsync(Guid currentUserId, Guid userId)
    {
        await _helper.GetUserOr404(currentUserId);

        if (currentUserId == userId)
        {
            _logger.LogInformation("User can't delete themself: {userId}", currentUserId);
            throw new BadRequestException("User can't delete themself");
        }

        var user = await _helper.GetUserOr404(userId);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _repository.DeleteAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("User successfully deleted: {userId}", userId);
            
            await _cache.RemoveDataAsync(GetUsersCachedKey());
            await _cache.RemoveDataAsync(GetUserCacheKeyById(userId));
            await _cache.RemoveDataAsync(GetGeneralAnalyticsCacheKey());

            _logger.LogInformation("User deleted from redis cache: {userId}", userId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while deleting the user profile: {Message}", ex.Message);
            throw new DatabaseException("Could not delete the user profile to the database.");
        }
    }

    public async Task<bool> ActivateOrDiactivateUserAsync(Guid currentUserId, Guid userId, bool isActive)
    {
        await _helper.GetUserOr404(currentUserId);
        
        if (currentUserId == userId)
        {
            _logger.LogInformation("User can't activate or diactivate themself: {userId}", currentUserId);
            throw new BadRequestException("User can't activate or diactivate for themself");
        }

        var user = await _helper.GetUserOr404(userId);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            user.isActive = isActive;

            await _repository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("User active status changed: {userId}", userId);

            await _cache.RemoveDataAsync(GetUsersCachedKey());
            await _cache.RemoveDataAsync(GetUserCacheKeyById(userId));

            _logger.LogInformation("User deleted from redis cache: {userId}", userId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while updating the user: {Message}", ex.Message);
            throw new DatabaseException("Could not update the user profile to the database.");
        }
    }

    public async Task<bool> AddRoleToUserAsync(Guid currentUserId, Guid userId, UserRole role)
    {
        await _helper.GetUserOr404(currentUserId);
        
        if (currentUserId == userId)
        {
            _logger.LogInformation("User can't add role for themself: {userId}", currentUserId);
            throw new BadRequestException("User can't add role for themself");
        }

        var user = await _helper.GetUserOr404(userId);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            user.AddRole(role);

            await _repository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Successfully added role to user: {userId}", userId);

            await _cache.RemoveDataAsync(GetUsersCachedKey());
            await _cache.RemoveDataAsync(GetUserCacheKeyById(userId));

            _logger.LogInformation("User deleted from redis cache: {userId}", userId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while updating the user: {Message}", ex.Message);
            throw new DatabaseException("Could not update the user profile to the database.");
        }
    }

    public async Task<bool> RemoveRoleFromUserAsync(Guid currentUserId, Guid userId, UserRole role)
    {
        await _helper.GetUserOr404(currentUserId);
        
        if (currentUserId == userId)
        {
            _logger.LogInformation("User can't remove role from themself: {userId}", currentUserId);
            throw new BadRequestException("User can't remove role from themself");
        }

        var user = await _helper.GetUserOr404(userId);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            user.RemoveRole(role);

            await _repository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Successfully removed role to user: {userId}", userId);

            await _cache.RemoveDataAsync(GetUsersCachedKey());
            await _cache.RemoveDataAsync(GetUserCacheKeyById(userId));

            _logger.LogInformation("User deleted from redis cache: {userId}", userId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while updating the user: {Message}", ex.Message);
            throw new DatabaseException("Could not update the user profile to the database.");
        }
    }
}