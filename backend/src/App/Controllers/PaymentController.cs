using App.DTOs;
using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


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

    [Authorize]
    [HttpPost("Order/{orderId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreatePayment(Guid orderId)
    {
        Guid userId = _helper.GetUserId();

        var payment = await _service.CreatePaymentAsync(userId, orderId);

        return Ok(payment);
    }

    [Authorize]
    [HttpGet("{paymentId:guid}/Order/{orderId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPayment(Guid orderId, Guid paymentId)
    {
        Guid userId = _helper.GetUserId();

        var payment = await _service.GetPaymentAsync(userId, orderId, paymentId);

        return Ok(payment);
    }

    [HttpPut("webhook")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookDto webhookDto)
    {
        
        await _service.ProcessWebhookAsync(webhookDto);
        
        return Ok(new { message = "Webhook processed successfully" });
    }
}