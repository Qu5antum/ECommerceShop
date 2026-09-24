using App.DTOs;
using App.Exceptions;
using App.Models;
using App.Repositories;
using App.Services.Caching;
using App.Transactions;
using Microsoft.EntityFrameworkCore;

namespace App.Services;


public interface IReviewService
{
    Task<ReviewResponseDto> CreateReviewAsync(Guid userId, Guid productId, CreateReviewDto createReviewDto);
    Task<bool> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto updateReviewDto);
    Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId);
    Task<List<ReviewResponseDto>> GetReviewsByProductId(Guid productId);
}


public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly ILogger<ReviewService> _logger;
    private readonly IHelperService _helper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCacheService _cache;

    private static string GetReviewsCacheKey(Guid productId)
    {
        return $"review:{productId}";
    }

    public ReviewService(IReviewRepository reviewRepository, ILogger<ReviewService> logger, IHelperService helper, IUnitOfWork unitOfWork, IRedisCacheService cache)
    {
        _reviewRepository = reviewRepository;
        _logger = logger;
        _helper = helper;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<ReviewResponseDto> CreateReviewAsync(Guid userId, Guid productId, CreateReviewDto createReviewDto)
    {
        await _unitOfWork.BeginTransactionAsync();
        await _helper.GetUserOr404(userId);
        await _helper.GetProductOr404(productId);

        if (createReviewDto.Rating < 1 || createReviewDto.Rating > 5)
        {
            _logger.LogWarning("Rating can be only from 1 to 5, product ID: {productId}, user ID: {userId}", productId, userId);
            throw new BadRequestException("Rating can be only from 1 to 5 rate");
        }

        var product = await _reviewRepository.GetReviewByUserAndProductId(userId, productId);

        if (product != null)
        {
            _logger.LogWarning("User already have review for this product, user ID: {userId}, product ID: {productId}", userId, productId);
            throw new BadRequestException("User already have revies for this product");
        }

        try
        {
            var newReview = new Review
            {
                UserId = userId,
                ProductId = productId,
                Rating = createReviewDto.Rating,
                Comment = createReviewDto.Comment
            };

            await _reviewRepository.CreateAsync(newReview);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Review successfully created: {reviewId}", newReview.Id);

            await _cache.RemoveDataAsync(GetReviewsCacheKey(productId));

            _logger.LogInformation("Reviews deleted from redis cache");

            return new ReviewResponseDto
            {
                Id = newReview.Id,
                UserId = newReview.UserId,
                ProductId = newReview.ProductId,
                Rating = newReview.Rating,
                Comment = newReview.Comment,
                CreatedAt = newReview.CreatedAt,
                UpdatedAt = newReview.UpdatedAt
            };
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while creating the review: {Message}.", ex.Message);
            throw new DatabaseException("Could not create the review to the database.");
        }
    }

    public async Task<bool> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewDto updateReviewDto)
    {
        await _unitOfWork.BeginTransactionAsync();
        await _helper.GetUserOr404(userId);
        
        var review = await _helper.GetReviewOr404(reviewId);

        if (review.UserId != userId)
        {
            _logger.LogWarning("Review does not belong to user, userId: {userId}, reviewId: {reviewId}", userId, reviewId);
            throw new BadRequestException("Review does not belong to user");
        }

        if (updateReviewDto.Rating < 1 && updateReviewDto.Rating > 5)
        {
            _logger.LogWarning("Rating can be only from 1 to 5 user ID: {userId}, reviewId: {reviewId}", userId, reviewId);
            throw new BadRequestException("Rating can be only from 1 to 5 rate");
        }

        try
        {
            if (updateReviewDto.Rating.HasValue)
            {
                review.Rating = updateReviewDto.Rating.Value;
            }

            if (updateReviewDto.Comment != null)
            {
                review.Comment = updateReviewDto.Comment;
            }

            await _reviewRepository.UpdateAsync(review);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Review sucessfully updated: {reviewId}", reviewId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while updating the review: {Message}.", ex.Message);
            throw new DatabaseException("Could not update the review to the database.");
        }
    }

    public async Task<bool> DeleteReviewAsync(Guid userId, Guid reviewId)
    {
        await _unitOfWork.BeginTransactionAsync();
        await _helper.GetUserOr404(userId);
        var review = await _helper.GetReviewOr404(reviewId);

        if (review.UserId != userId)
        {
            _logger.LogWarning("Review does not belong to user, userId: {userId}, reviewId: {reviewId}", userId, reviewId);
            throw new BadRequestException("Review does not belong to user");
        }

        try
        {
            await _reviewRepository.DeleteAsync(review);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Review successfully deleted: {reviewId}", reviewId);

            return true;
        }
        catch (DbUpdateException ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "A database error occurred while deleting the review: {Message}.", ex.Message);
            throw new DatabaseException("Could not delete the review to the database.");
        }
    }

    public async Task<List<ReviewResponseDto>> GetReviewsByProductId(Guid productId)
    {
        await _helper.GetProductOr404(productId);

        var cacheKey = GetReviewsCacheKey(productId);

        var cachedReviews = await _cache.GetDataAsync<List<ReviewResponseDto>>(cacheKey);

        if (cachedReviews is not null)
        {
            _logger.LogInformation("Review retrieved from redis cache");
            return cachedReviews;
        }

        var reviews = await _reviewRepository.GetReviewsByProductId(productId);

        var result = reviews.Select(review => new ReviewResponseDto
        {
            Id = review.Id,
            UserId = review.UserId,
            ProductId = review.ProductId,
            Comment = review.Comment,
            Rating = review.Rating,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        }).ToList();

        await _cache.SetDataAsync(
            cacheKey,
            result,
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("Successfull response of reviews by product: {productId}", productId);
        
        return result;
    }
}