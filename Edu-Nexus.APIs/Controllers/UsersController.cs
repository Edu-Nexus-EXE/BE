using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Auth.Commands;
using Edu_Nexus.Application.Features.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var result = await _mediator.Send(new GetCurrentUserQuery());
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 USER_NOT_FOUND")
        {
            return NotFound(new { error = ex.Message });
        }
    }
    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateCurrentUserRequest request)
    {
        try
        {
            var result = await _mediator.Send(new UpdateCurrentUserCommand(request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 USER_NOT_FOUND")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_SLUG")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_SLUG", message = "Portfolio slug is invalid" } });
        }
        catch (Exception ex) when (ex.Message == "409 SLUG_TAKEN")
        {
            return Conflict(new { error = new { code = "SLUG_TAKEN", message = "Portfolio slug is already taken" } });
        }
    }
}
