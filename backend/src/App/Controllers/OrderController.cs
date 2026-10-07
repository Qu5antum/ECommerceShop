using App.Services;
using App.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Enum;

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
    public async Task<IActionResult> CreateOrder()
    {
        Guid userId = _helper.GetUserId();

        var order = await _service.CreateOrderAsync(userId);

        return Ok(order);
    }

    [HttpGet("Orders")]
    public async Task<IActionResult> GetOrdersOfUser()
    {
        Guid userId = _helper.GetUserId();

        var orders = await _service.GetOrdersByUserIdAsync(userId);

        return Ok(orders);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderOfUser(Guid orderId)
    {
        Guid userId = _helper.GetUserId();

        var order = await _service.GetOrderByUserIdAsync(userId, orderId);

        return Ok(order);
    }

    [HttpDelete("{orderId:guid}/Cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderId)
    {
        Guid userId = _helper.GetUserId();

        await _service.CancelOrderAsync(userId, orderId);

        return Ok("Order cancelled successfully");
    }

    [Authorize(Roles = "Seller")]
    [HttpGet("Orders/Seller")]
    public async Task<IActionResult> GetOrdersOfSeller()
    {
        Guid userId = _helper.GetUserId();

        var orders = await _service.GetOrdersOfSellerAsync(userId);

        return Ok(orders);
    }

    [Authorize(Roles = "Seller")]
    [HttpGet("{orderId:guid}/Detail")]
    public async Task<IActionResult> GetOrderWithItemsAndUser(Guid orderId)
    {
        Guid userId = _helper.GetUserId();

        var order = await _service.GetOrderOfSellerWithItemAndUserAsync(userId, orderId);

        return Ok(order);
    }

    [Authorize(Roles = "Moderator, Admin, Manager")]
    [HttpPut("{orderId:guid}/Status")]
    public async Task<IActionResult> UpdateStatusOfOrder(Guid orderId, UpdateOrderStatusDto orderStatusDto)
    {
        await _service.UpdateOrderStatusAsync(orderId, orderStatusDto);

        return Ok("Order status successfully updated");
    }

    [Authorize(Roles = "Moderator, Admin, Manager")]
    [HttpGet("Orders/Admin")]
    public async Task<IActionResult> GetOrders(OrderStatus? status)
    {
        var orders = await _service.GetOrdersForAdminAsync(status);

        return Ok(orders);
    }

    [Authorize(Roles = "Moderator, Admin, Manager")]
    [HttpGet("{orderId:guid}/Admin")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        var order = await _service.GetOrderAdminAsync(orderId);

        return Ok(order);
    }

    [Authorize(Roles = "Moderator, Admin, Manager")]
    [HttpGet("User/{userId:guid}/Admin")]
    public async Task<IActionResult> GetOrdersByUserId(Guid userId)
    {
        var orders = await _service.GetOrdersByUserIdAsync(userId);

        return Ok(orders);
    }

    [Authorize(Roles = "Moderator, Admin, Manager")]
    [HttpGet("Admin/Date")]
    public async Task<IActionResult> GetOrdersFromDateToDate(DateTime fromDate, DateTime toDate)
    {
        var products = await _service.GetOrdersFromDateToDateAsync(fromDate, toDate);

        return Ok(products);
    }
}