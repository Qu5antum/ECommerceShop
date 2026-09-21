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
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateReview(Guid productId, CreateReviewDto createReviewDto)
    {
        Guid userId = _helper.GetUserId();

        var review = await _service.CreateReviewAsync(userId, productId, createReviewDto);

        return Ok(review);
    }

    [HttpPut("{reviewId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateReview(Guid reviewId, UpdateReviewDto updateReviewDto)
    {
        Guid userId = _helper.GetUserId();

        await _service.UpdateReviewAsync(userId, reviewId, updateReviewDto);

        return Ok("Review successfully updated");
    }

    [HttpDelete("{reviewId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        Guid userId = _helper.GetUserId();

        await _service.DeleteReviewAsync(userId, reviewId);

        return Ok("Review successfully deleted");
    }

    [HttpGet("{productId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetReviewsByProduct(Guid productId)
    {
        var reviews = await _service.GetReviewsByProductId(productId);

        return Ok(reviews);
    }
}