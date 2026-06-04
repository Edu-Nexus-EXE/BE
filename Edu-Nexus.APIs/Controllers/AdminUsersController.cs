using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Admin.Users.Commands;
using Edu_Nexus.Application.Features.Admin.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Roles = "admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? tier,
        [FromQuery] bool? isBanned,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminUsersQuery(search, tier, isBanned, page, pageSize));
            return Ok(new { data = result.Data, pagination = result.Pagination });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_TIER_FILTER")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_TIER_FILTER", message = "tier phải là free hoặc student" } });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminUserDetailQuery(id));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "404 USER_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "USER_NOT_FOUND", message = "Không tìm thấy user." } });
        }
    }

    [HttpPatch("{id:guid}/ban")]
    public async Task<IActionResult> SetBan(Guid id, [FromBody] SetUserBanRequest request)
    {
        try
        {
            await _mediator.Send(new SetUserBanCommand(id, request));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 USER_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "USER_NOT_FOUND", message = "Không tìm thấy user." } });
        }
        catch (Exception ex) when (ex.Message == "422 CANNOT_BAN_SELF")
        {
            return UnprocessableEntity(new { error = new { code = "CANNOT_BAN_SELF", message = "Admin không thể ban chính mình." } });
        }
    }

    [HttpPost("{id:guid}/activate-subscription")]
    public async Task<IActionResult> ActivateSubscription(Guid id, [FromBody] ActivateUserSubscriptionRequest request)
    {
        try
        {
            var result = await _mediator.Send(new ActivateUserSubscriptionCommand(id, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 USER_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "USER_NOT_FOUND", message = "Không tìm thấy user." } });
        }
        catch (Exception ex) when (ex.Message == "404 STUDENT_TIER_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "STUDENT_TIER_NOT_FOUND", message = "Gói Student chưa được seed trong subscription_tiers." } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_DURATION")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_DURATION", message = "durationMonths phải nằm trong 1-24." } });
        }
    }

    [HttpPost("{id:guid}/revoke-subscription")]
    public async Task<IActionResult> RevokeSubscription(Guid id)
    {
        try
        {
            await _mediator.Send(new RevokeUserSubscriptionCommand(id));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 USER_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "USER_NOT_FOUND", message = "Không tìm thấy user." } });
        }
        catch (Exception ex) when (ex.Message == "422 NO_ACTIVE_SUBSCRIPTION")
        {
            return UnprocessableEntity(new { error = new { code = "NO_ACTIVE_SUBSCRIPTION", message = "User chưa có subscription Active để thu hồi." } });
        }
    }
}
