using App.Services;
using App.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly Helper _helper;

    public OrderController(IOrderService service, Helper helper)
    {
        _service = service;
        _helper = helper;
    }

    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateOrder()
    {
        Guid userId = _helper.GetUserId();

        var order = await _service.CreateOrderAsync(userId);

        return Ok(order);
    }

    [HttpGet("Orders")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetOrders()
    {
        Guid userId = _helper.GetUserId();

        var orders = await _service.GetOrdersByUserIdAsync(userId);

        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        Guid userId = _helper.GetUserId();

        var order = await _service.GetOrderByUserIdAsync(userId, orderId);

        return Ok(order);
    }

    [HttpDelete("{orderId:guid}/Cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CancelOrder(Guid orderId)
    {
        Guid userId = _helper.GetUserId();

        await _service.CancelOrderAsync(userId, orderId);

        return Ok("Order cancelled successfully");
    }

    [Authorize(Roles = "Seller")]
    [HttpDelete("Admin/Orders")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetOrdersOfSeller()
    {
        Guid userId = _helper.GetUserId();

        var orders = await _service.GetOrdersOfSellerAsync(userId);

        return Ok(orders);
    }

    [Authorize(Roles = "Moderator, Admin, Manager")]
    [HttpPut("{orderId:guid}/Status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateStatusOfOrder(Guid orderId, UpdateOrderStatusDto orderStatusDto)
    {
        await _service.UpdateOrderStatusAsync(orderId, orderStatusDto);

        return Ok("Order status successfully updated");
    }
}