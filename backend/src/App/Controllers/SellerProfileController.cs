using App.DTOs;
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
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateSellerProfile(SellerProfileCreateDto profileCreateDto)
    {
        Guid userId = _helper.GetUserId();
        var sellerProfile = await _service.CreateSellerProfileAsync(userId, profileCreateDto);

        return Ok(sellerProfile);
    }

    [HttpPut("{profileId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> UpdateSellerProfile(Guid profileId, SellerProfileUpdateDto profileUpdateDto)
    {
        Guid userId = _helper.GetUserId();
        await _service.UpdateSellerProfileAsync(userId, profileId, profileUpdateDto);

        return Ok("Seller profile successfully updated");
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetCurrentUserSellerProfile()
    {
        Guid userId = _helper.GetUserId();
        var sellerProfile = await _service.GetUserSellerProfileAsync(userId);

        return Ok(sellerProfile);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetUserSellerProfile(Guid userId)
    {
        var sellerProfile = await _service.GetUserSellerProfileAsync(userId);

        return Ok(sellerProfile);
    }
}
