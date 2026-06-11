using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Admin.SubscriptionTiers.Commands;
using Edu_Nexus.Application.Features.Admin.SubscriptionTiers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("admin/subscription-tiers")]
[Authorize(Roles = "admin")]
public class AdminSubscriptionTiersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminSubscriptionTiersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await _mediator.Send(new GetAdminSubscriptionTiersQuery());
        return Ok(new { data = result });
    }

    [HttpPut("{tierCode}")]
    public async Task<IActionResult> Update(string tierCode, [FromBody] UpdateSubscriptionTierRequest request)
    {
        try
        {
            var result = await _mediator.Send(new UpdateSubscriptionTierCommand(tierCode, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 TIER_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "TIER_NOT_FOUND", message = "tierCode phải là free hoặc student." } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_PRICE")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_PRICE", message = "priceMonthly không được âm." } });
        }
        catch (Exception ex) when (ex.Message.StartsWith("422 INVALID_QUOTA"))
        {
            var parts = ex.Message.Split('|');
            return UnprocessableEntity(new
            {
                error = new
                {
                    code = "INVALID_QUOTA",
                    field = parts.Length > 1 ? parts[1] : null,
                    message = "Quota phải >= -1 (-1 = unlimited)."
                }
            });
        }
    }
}
