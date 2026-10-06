using App.DTOs;
using App.Enum;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SellerProfileController : ControllerBase
{
    private readonly ISellerProfileService _service;
    private readonly Helper _helper;

    public SellerProfileController(ISellerProfileService service, Helper helper)
    {
        _service = service;
        _helper = helper;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSellerProfile(SellerProfileCreateDto profileCreateDto)
    {
        Guid userId = _helper.GetUserId();
        var sellerProfile = await _service.CreateSellerProfileAsync(userId, profileCreateDto);

        return Ok(sellerProfile);
    }

    [HttpPut("{profileId:guid}")]
    public async Task<IActionResult> UpdateSellerProfile(Guid profileId, SellerProfileUpdateDto profileUpdateDto)
    {
        Guid userId = _helper.GetUserId();
        await _service.UpdateSellerProfileAsync(userId, profileId, profileUpdateDto);

        return Ok("Seller profile successfully updated");
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentUserSellerProfile()
    {
        Guid userId = _helper.GetUserId();
        var sellerProfile = await _service.GetCurrentUserProfileAsync(userId);

        return Ok(sellerProfile);
    }

    [HttpGet("{sellerId:guid}/Image")]
    public async Task<IActionResult> GetSellerProfileImage(Guid sellerId)
    {
        var file = await _service.GetSellerProfileImageAsync(sellerId);

        if (file is null)
        {
            return NotFound("Seller profile image not found.");
        }

        return File(file.Value.FileStream, file.Value.ContentType);
    }

    [HttpGet("{sellerId:guid}")]
    public async Task<IActionResult> GetUserSellerProfile(Guid sellerId)
    {
        var sellerProfile = await _service.GetUserSellerProfileAsync(sellerId);

        return Ok(sellerProfile);
    }

    [HttpGet("{sellerId:guid}/Preview")]
    public async Task<IActionResult> GetSellerProfilePreview(Guid sellerId)
    {
        var sellerPreview = await _service.GetSellerProfilePreviewAsync(sellerId);

        return Ok(sellerPreview);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("Sellers")]
    public async Task<IActionResult> GetSellers(SellerStatus status)
    {
        var sellers = await _service.GetSellersAsync(status);

        return Ok(sellers);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpPut("{sellerId:guid}/Status")]
    public async Task<IActionResult> UpdateStatusOfSellerProfile(Guid sellerId, SellerStatus status)
    {
        Guid userId = _helper.GetUserId();

        await _service.UpdateStatusOfSellerProfile(userId, sellerId, status);

        return Ok("Status of seller profile successfully updated");
    }
}
