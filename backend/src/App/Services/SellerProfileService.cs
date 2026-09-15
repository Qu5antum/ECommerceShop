using App.DTOs;
using App.Enum;
using App.Exceptions;
using App.Models;
using App.Repositories;
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
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SellerProfileService> _logger;
    private readonly IHelperService _helper;

    public SellerProfileService(ISellerProfileRepository profileRepository, IUserRepository userRepository, ILogger<SellerProfileService> logger, IHelperService helper)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
        _logger = logger;
        _helper = helper;
    }

    public async Task<SellerProfileResponseDto> CreateSellerProfileAsync(Guid currentUserId, SellerProfileCreateDto profileCreateDto)
    {
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
            _logger.LogError(ex, "A database error occurred while creating the seller profile.");
            throw new DatabaseException("Could not save the seller profile to the database.");
        }
    }

    public async Task<bool> UpdateSellerProfileAsync(Guid userId, Guid profileId, SellerProfileUpdateDto profileUpdateDto)
    {
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

        sellerProfile.UpdatedAt = DateTime.UtcNow;

        await _profileRepository.UpdateAsync(sellerProfile);

        return true;
    }

    public async Task<SellerProfileResponseDto> GetUserSellerProfileAsync(Guid userId)
    {
        await _helper.GetUserOr404(userId);
        
        var sellerProfile = await _profileRepository.GetSellerProfileByUserIdAsync(userId);

        if (sellerProfile == null)
        {
            _logger.LogWarning("User don't have seller profile: {userId}", userId);
            throw new NotFoundException("User don't have seller profile");
        }

        return new SellerProfileResponseDto
        {
            Id = sellerProfile.Id,
            userId = sellerProfile.userId,
            StoreName = sellerProfile.StoreName,
            Description = sellerProfile.Description,
            IsApproved = sellerProfile.IsApproved,
            CreatedAt = sellerProfile.CreatedAt,
            UpdatedAt = sellerProfile.UpdatedAt
        };
    }
}