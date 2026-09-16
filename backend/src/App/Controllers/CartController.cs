using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _service;
    private readonly Helper _helper;

    public CartController(ICartService service, Helper helper)
    {
        _service = service;
        _helper = helper;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCart()
    {
        Guid userId = _helper.GetUserId();

        var cart = await _service.GetCartAsync(userId);

        return Ok(cart);
    }
}