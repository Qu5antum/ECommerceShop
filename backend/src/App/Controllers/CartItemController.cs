using App.DTOs;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartItemController : ControllerBase
{
    private readonly ICartItemService _itemService;
    private readonly Helper _helper;

    public CartItemController(ICartItemService itemService, Helper helper)
    {
        _itemService = itemService;
        _helper = helper;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateCartItem(CartItemCreateDto itemCreateDto)
    {
        Guid userId = _helper.GetUserId();

        var cartItem = await _itemService.CreateCartItemAsync(userId, itemCreateDto);

        return Ok(cartItem);
    }

    [Authorize]
    [HttpPut("{itemId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateCartItem(Guid itemId, CartItemUpdateDto itemUpdateDto)
    {
        Guid userId = _helper.GetUserId();

        await _itemService.UpdateCartItemAsync(userId, itemId, itemUpdateDto);

        return Ok("Cart item successfully updated");
    }

    [Authorize]
    [HttpDelete("{itemId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteCartItem(Guid itemId)
    {
        Guid userId = _helper.GetUserId();

        await _itemService.DeleteItemFromCartAsync(userId, itemId);

        return Ok("Item successfully deleted from cart");
    }

    [Authorize]
    [HttpGet("CartItems")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAllItemsFromCart()
    {
        Guid userId = _helper.GetUserId();

        var cartItems = await _itemService.GetAllItemsInCartAsync(userId);

        return Ok(cartItems);
    }

    [Authorize]
    [HttpGet("{itemId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetItemInCartById(Guid itemId)
    {
        Guid userId = _helper.GetUserId();

        var cartItem = await _itemService.GetItemInCartByIdAsync(userId, itemId);

        return Ok(cartItem);
    }
}