using App.DTOs;
using App.Enum;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly Helper _helper;

    public PaymentController(IPaymentService service, Helper helper)
    {
        _service = service;
        _helper = helper;
    }

    [HttpPost("Order/{orderId:guid}")]
    public async Task<IActionResult> CreatePayment(Guid orderId)
    {
        Guid userId = _helper.GetUserId();

        var payment = await _service.CreatePaymentAsync(userId, orderId);

        return Ok(payment);
    }

    [HttpGet("{paymentId:guid}/Order/{orderId:guid}")]
    public async Task<IActionResult> GetPayment(Guid orderId, Guid paymentId)
    {
        Guid userId = _helper.GetUserId();

        var payment = await _service.GetPaymentAsync(userId, orderId, paymentId);

        return Ok(payment);
    }
    
    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpPut("Admin/Webhook")]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookDto webhookDto)
    {
        
        await _service.ProcessWebhookAsync(webhookDto);
        
        return Ok(new { message = "Webhook processed successfully" });
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("Payments")]
    public async Task<IActionResult> GetPaymentsAdmin(PaymentStatus? status)
    {
        var payments = await _service.GetPaymentsAdminAsync(status);

        return Ok(payments);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPaymentAdmin(Guid paymentId)
    {
        var payment = await _service.GetPaymentAdminAsync(paymentId);

        return Ok(payment);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("Payments/Statistics")]
    public async Task<IActionResult> GetPaymentsStatistics()
    {
        var statistics = await _service.GetPaymentsStatisticAsync();

        return Ok(statistics);
    }
}