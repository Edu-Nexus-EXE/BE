using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Subscriptions.Commands;
using Edu_Nexus.Application.Features.Subscriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(IMediator mediator, ILogger<SubscriptionController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>GET /subscription/tiers — Danh sách gói cước (public, dùng cho Pricing page)</summary>
    [HttpGet("tiers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTiers(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubscriptionTiersQuery(), ct);
        return Ok(new { data = result });
    }

    /// <summary>GET /subscription/me — Subscription + quota usage hiện tại (FR8.2)</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMySubscription(CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetMySubscriptionQuery(), ct);
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message.StartsWith("401"))
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED" } });
        }
    }

    /// <summary>POST /subscription/orders — Tạo order thanh toán SePay + trả VietQR (FR8.4)</summary>
    [HttpPost("orders")]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreatePaymentOrderCommand(request), ct);
            return StatusCode(201, new { data = result });
        }
        catch (Exception ex) when (ex.Message.StartsWith("401"))
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED" } });
        }
        catch (Exception ex) when (ex.Message.StartsWith("404"))
        {
            return NotFound(new { error = new { code = ex.Message[4..] } });
        }
        catch (Exception ex) when (ex.Message.StartsWith("422"))
        {
            return UnprocessableEntity(new { error = new { code = ex.Message[4..] } });
        }
    }

    /// <summary>POST /subscription/webhook — Webhook nhận callback thanh toán từ SePay (FR8.4)</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook([FromBody] SepayWebhookPayload payload, CancellationToken ct)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString().Trim();
            var result = await _mediator.Send(new HandleSepayWebhookCommand(payload, authHeader), ct);
            return Ok(new { success = result });
        }
        catch (Exception ex) when (ex.Message.StartsWith("401"))
        {
            return Unauthorized(new { error = new { code = "UNAUTHORIZED" } });
        }
        catch (Exception ex)
        {
            // Server-side failure (DB down, NPE, ...). Returning 400 with the raw
            // exception message would leak internals to SePay and prevent retries
            // for transient failures. Log and return 500 so SePay retries the
            // delivery and ops sees the stack trace.
            _logger.LogError(ex, "SePay webhook handler failed for txId={TxId} content='{Content}'",
                payload?.Id, payload?.Content);
            return StatusCode(500, new { error = new { code = "INTERNAL_ERROR" } });
        }
    }
}

