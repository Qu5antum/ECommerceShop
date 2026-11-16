using Moq;
using Microsoft.Extensions.Logging;

using App.Repositories;
using App.Services;
using App.Transactions;
using App.Services.Caching;
using App.Models;
using App.Exceptions;
using App.DTOs;
using Microsoft.EntityFrameworkCore;


public class ReviewServiceTests
{
    private readonly Mock<IReviewRepository> _reviewRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRedisCacheService> _cacheMock; 
    private readonly Mock<ILogger<ReviewService>> _loggerMock;
    private readonly Mock<IHelperService> _helper;
    private readonly ReviewService _reviewService;

    public ReviewServiceTests()
    {
        _reviewRepositoryMock = new Mock<IReviewRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<IRedisCacheService>();
        _loggerMock = new Mock<ILogger<ReviewService>>();
        _helper = new Mock<IHelperService>();

        _reviewService = new ReviewService(
            _reviewRepositoryMock.Object,
            _loggerMock.Object,
            _helper.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task CreateReviewAsync_WhenRatingIsInvalid_ThrowsBadRequestException(int invalidRating)
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testProduct = ProductTestHelper.CreateTestProduct(Id: productId);

        var dto = new CreateReviewDto
        {
            Rating = invalidRating,
            Comment = "Great product!"
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetProductOr404(productId))
            .ReturnsAsync(testProduct);

        Func<Task> action = async () => await _reviewService.CreateReviewAsync(userId, productId, dto);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Rating can be only from 1 to 5 rate");

        _reviewRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Review>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenUserAlreadyReviewedProduct_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testProduct = ProductTestHelper.CreateTestProduct(Id: productId);

        var existingReview = new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            Rating = 5,
            Comment = "Existing review"
        };

        var dto = new CreateReviewDto
        {
            Rating = 4,
            Comment = "Another review attempt"
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetProductOr404(productId))
            .ReturnsAsync(testProduct);

        _reviewRepositoryMock.Setup(r => r.GetReviewByUserAndProductId(userId, productId))
            .ReturnsAsync(existingReview);

        Func<Task> action = async () => await _reviewService.CreateReviewAsync(userId, productId, dto);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("User already have revies for this product");

        _reviewRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Review>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenValidRequest_CreatesReviewCommitsAndInvalidatesCache()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testProduct = ProductTestHelper.CreateTestProduct(Id: productId);

        var dto = new CreateReviewDto
        {
            Rating = 5,
            Comment = "Excellent service and product!"
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetProductOr404(productId))
            .ReturnsAsync(testProduct);

        _reviewRepositoryMock.Setup(r => r.GetReviewByUserAndProductId(userId, productId))
            .ReturnsAsync((Review?)null);

        var result = await _reviewService.CreateReviewAsync(userId, productId, dto);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.ProductId.Should().Be(productId);
        result.Rating.Should().Be(dto.Rating);
        result.Comment.Should().Be(dto.Comment);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _reviewRepositoryMock.Verify(r => r.CreateAsync(It.Is<Review>(rev => rev.UserId == userId && rev.ProductId == productId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateReviewAsync_WhenDbUpdateExceptionOccurs_RollsBackAndThrowsDatabaseException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testProduct = ProductTestHelper.CreateTestProduct(Id: productId);

        var dto = new CreateReviewDto
        {
            Rating = 4,
            Comment = "Good product"
        };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetProductOr404(productId))
            .ReturnsAsync(testProduct);

        _reviewRepositoryMock.Setup(r => r.GetReviewByUserAndProductId(userId, productId))
            .ReturnsAsync((Review?)null);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Database constraint error", new Exception()));

        Func<Task> action = async () => await _reviewService.CreateReviewAsync(userId, productId, dto);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not create the review to the database.");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateReviewAsync_WhenReviewDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var foreignReview = ReviewTestHelper.CreateTestReview(Id: reviewId, UserId: otherUserId);

        var updateDto = new UpdateReviewDto { Rating = 4, Comment = "Updated comment" };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetReviewOr404(reviewId))
            .ReturnsAsync(foreignReview);

        Func<Task> action = async () => await _reviewService.UpdateReviewAsync(userId, reviewId, updateDto);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Review does not belong to user");

        _reviewRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Review>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateReviewAsync_WhenValidRequest_UpdatesReviewCommitsAndInvalidatesCache()
    {
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testReview = ReviewTestHelper.CreateTestReview(Id: reviewId, UserId: userId, Rating: 2, Comment: "Old comment");

        var updateDto = new UpdateReviewDto { Rating = 5, Comment = "New amazing comment!" };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetReviewOr404(reviewId))
            .ReturnsAsync(testReview);

        var result = await _reviewService.UpdateReviewAsync(userId, reviewId, updateDto);

        result.Should().BeTrue();
        testReview.Rating.Should().Be(5);
        testReview.Comment.Should().Be("New amazing comment!");

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _reviewRepositoryMock.Verify(r => r.UpdateAsync(testReview), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateReviewAsync_WhenDbUpdateExceptionOccurs_RollsBackAndThrowsDatabaseException()
    {
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testReview = ReviewTestHelper.CreateTestReview(Id: reviewId, UserId: userId);

        var updateDto = new UpdateReviewDto { Rating = 4 };

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetReviewOr404(reviewId))
            .ReturnsAsync(testReview);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Database error", new Exception()));

        Func<Task> action = async () => await _reviewService.UpdateReviewAsync(userId, reviewId, updateDto);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not update the review to the database.");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteReviewAsync_WhenReviewDoesNotBelongToUser_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var foreignReview = ReviewTestHelper.CreateTestReview(Id: reviewId, UserId: otherUserId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetReviewOr404(reviewId))
            .ReturnsAsync(foreignReview);

        Func<Task> action = async () => await _reviewService.DeleteReviewAsync(userId, reviewId);

        await action.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Review does not belong to user");

        _reviewRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Review>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteReviewAsync_WhenValidRequest_DeletesReviewCommitsAndInvalidatesCache()
    {
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testReview = ReviewTestHelper.CreateTestReview(Id: reviewId, UserId: userId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetReviewOr404(reviewId))
            .ReturnsAsync(testReview);

        var result = await _reviewService.DeleteReviewAsync(userId, reviewId);

        result.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _reviewRepositoryMock.Verify(r => r.DeleteAsync(testReview), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);

        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task DeleteReviewAsync_WhenDbUpdateExceptionOccurs_RollsBackAndThrowsDatabaseException()
    {
        var userId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var testUser = AuthTestHelper.CreateTestUser(Id: userId);
        var testReview = ReviewTestHelper.CreateTestReview(Id: reviewId, UserId: userId);

        _helper.Setup(h => h.GetUserOr404(userId))
            .ReturnsAsync(testUser);

        _helper.Setup(h => h.GetReviewOr404(reviewId))
            .ReturnsAsync(testReview);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Database constraint error", new Exception()));

        Func<Task> action = async () => await _reviewService.DeleteReviewAsync(userId, reviewId);

        await action.Should().ThrowAsync<DatabaseException>()
            .WithMessage("Could not delete the review to the database.");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        _cacheMock.Verify(c => c.RemoveDataAsync(It.IsAny<string>()), Times.Never);
    }
}