using App.DTOs;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _service;
    private readonly Helper _helper;

    public ReviewController(IReviewService service, Helper helper)
    {
        _service = service;
        _helper = helper;
    }

    [HttpPost("{productId:guid}")]
    public async Task<IActionResult> CreateReview(Guid productId, CreateReviewDto createReviewDto)
    {
        Guid userId = _helper.GetUserId();

        var review = await _service.CreateReviewAsync(userId, productId, createReviewDto);

        return Ok(review);
    }

    [HttpPut("{reviewId:guid}")]
    public async Task<IActionResult> UpdateReview(Guid reviewId, UpdateReviewDto updateReviewDto)
    {
        Guid userId = _helper.GetUserId();

        await _service.UpdateReviewAsync(userId, reviewId, updateReviewDto);

        return Ok("Review successfully updated");
    }

    [HttpDelete("{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        Guid userId = _helper.GetUserId();

        await _service.DeleteReviewAsync(userId, reviewId);

        return Ok("Review successfully deleted");
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetReviewsByProduct(Guid productId)
    {
        var reviews = await _service.GetReviewsByProductId(productId);

        return Ok(reviews);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("Admin/Reviews")]
    public async Task<IActionResult> GetReviewsAdmin()
    {
        var reviews = await _service.GetAllReviewsAdminAsync();

        return Ok(reviews);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("{reviewId:guid}/Admin")]
    public async Task<IActionResult> GetReviewById(Guid reviewId)
    {
        var review = await _service.GetReviewByIdAdminAsync(reviewId);

        return Ok(review);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpDelete("{reviewId:guid}/Admin")]
    public async Task<IActionResult> DeleteReviewById(Guid reviewId)
    {
        await _service.DeleteReviewAdminAsync(reviewId);

        return Ok("Review successfully deleted");
    }
}