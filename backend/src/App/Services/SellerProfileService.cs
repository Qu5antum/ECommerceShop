using App.DTOs;
using App.Enum;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Services.Caching;
using App.Transactions;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface ISellerProfileService
{
    Task<SellerProfileResponseDto> CreateSellerProfileAsync(Guid currentUserId, SellerProfileCreateDto profileCreateDto);
    Task<SellerProfileResponseDto> GetUserSellerProfileAsync(Guid userId);
    Task<bool> UpdateSellerProfileAsync(Guid userId, Guid profileId, SellerProfileUpdateDto profileUpdateDto);
}


public class SellerProfileService : ISellerProfileService
{
    private readonly ISellerProfileRepository _profileRepository;
    private readonly ILogger<SellerProfileService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCacheService _cache;

    private static string GetSellerProfileCacheKey(Guid userId)
    {
        return $"seller:{userId}";
    }

    public SellerProfileService(ISellerProfileRepository profileRepository, ILogger<SellerProfileService> logger, IHelperService helper, IUnitOfWork unitOfWork, IRedisCacheService cache)
    {
        _profileRepository = profileRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<SellerProfileResponseDto> CreateSellerProfileAsync(Guid currentUserId, SellerProfileCreateDto profileCreateDto)
    {
        await _unitOfWork.BeginTransactionAsync();

        var user = await _helper.GetUserOr404(currentUserId);

        var sellerProfileOfUser = await _profileRepository.GetSellerProfileByUserIdAsync(currentUserId);

        if (sellerProfileOfUser != null)
        {
            _logger.LogWarning("User already have seller profile: {userId}", currentUserId);
            throw new AlreadyExistsException("User already have seller profile");
        }

        try
        {
            var storeNameIsTaken = await _profileRepository.IsStoreNameTakenAsync(profileCreateDto.StoreName);

            if (storeNameIsTaken)
            {
                _logger.LogWarning("Store name is already taken, Store name: {StoreName}, userID: {userId}", profileCreateDto.StoreName, currentUserId);
                throw new AlreadyExistsException("Store name is already taken");
            }

            var newSellerProfile = new SellerProfile
            {
                userId = user.Id,
                StoreName = profileCreateDto.StoreName,
                Description = profileCreateDto.Description,
                IsApproved = false
            };

            user.AddRole(UserRole.Seller);

            await _profileRepository.CreateAsync(newSellerProfile);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Seller Prorile created successfully: {profileId}", newSellerProfile.Id);

            return new SellerProfileResponseDto
            {
                userId = newSellerProfile.Id,
                StoreName = newSellerProfile.StoreName,
                Description = newSellerProfile.Description,
                IsApproved = newSellerProfile.IsApproved
            };
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while creating the seller profile.");
            throw new DatabaseException("Could not save the seller profile to the database.");
        }
    }

    public async Task<bool> UpdateSellerProfileAsync(Guid userId, Guid profileId, SellerProfileUpdateDto profileUpdateDto)
    {
        await _unitOfWork.BeginTransactionAsync();
        await _helper.GetUserOr404(userId);

        var sellerProfile = await _profileRepository.GetByIdAsync(profileId);

        if (sellerProfile == null)
        {
            _logger.LogWarning("Seller profile not found by id: {profileId}", profileId);
            throw new NotFoundException("Seller profile not found");
        }

        if (profileUpdateDto.StoreName != null)
        {
            sellerProfile.StoreName = profileUpdateDto.StoreName;
        }
        if(profileUpdateDto.Description != null)
        {
            sellerProfile.Description = profileUpdateDto.Description;
        }
        try
        {
            sellerProfile.UpdatedAt = DateTime.UtcNow;

            await _profileRepository.UpdateAsync(sellerProfile);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Seller profile successfully updated: {userId}", userId);

            await _cache.RemoveDataAsync(GetSellerProfileCacheKey(userId));

            _logger.LogInformation("Seller profile deleted from redis cache: {userId}", userId);
            
            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while updating the seller profile.");
            throw new DatabaseException("Could not update the seller profile to the database.");
        }
    }

    public async Task<SellerProfileResponseDto> GetUserSellerProfileAsync(Guid userId)
    {
        await _helper.GetUserOr404(userId);

        var cacheKey = GetSellerProfileCacheKey(userId);

        var cachedSellerProfile = await _cache.GetDataAsync<SellerProfileResponseDto>(cacheKey);

        if (cachedSellerProfile is not null)
        {
            _logger.LogInformation("Seller profile retrieved from redis cache");
            return cachedSellerProfile;
        }
        
        var sellerProfile = await _profileRepository.GetSellerProfileByUserIdAsync(userId);

        if (sellerProfile == null)
        {
            _logger.LogWarning("User don't have seller profile: {userId}", userId);
            throw new NotFoundException("User don't have seller profile");
        }

        var result = new SellerProfileResponseDto
        {
            Id = sellerProfile.Id,
            userId = sellerProfile.userId,
            StoreName = sellerProfile.StoreName,
            Description = sellerProfile.Description,
            IsApproved = sellerProfile.IsApproved,
            CreatedAt = sellerProfile.CreatedAt,
            UpdatedAt = sellerProfile.UpdatedAt
        };

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("Successfull response of seller profile: {userId}", userId);

        return result;
    }
}