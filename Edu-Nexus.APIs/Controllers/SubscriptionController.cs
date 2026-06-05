using Edu_Nexus.Application.Features.Subscriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /subscription/tiers — Danh sách gói cước (public, dùng cho Pricing page)</summary>
    [HttpGet("tiers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTiers(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubscriptionTiersQuery(), ct);
        return Ok(new { data = result });
    }
}
