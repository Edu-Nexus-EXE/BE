using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Auth.Commands;
using Edu_Nexus.Application.Features.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _mediator.Send(new RegisterCommand(request));
            return StatusCode(201, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "409 EMAIL_EXISTS")
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _mediator.Send(new LoginQuery(request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 INVALID_CREDENTIALS")
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message == "403 ACCOUNT_BANNED")
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var result = await _mediator.Send(new GoogleLoginCommand(request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 INVALID_GOOGLE_TOKEN")
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message == "403 ACCOUNT_BANNED")
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] TokenRefreshRequest request)
    {
        try
        {
            var result = await _mediator.Send(new RefreshTokenCommand(request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 INVALID_TOKEN")
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex) when (ex.Message == "403 ACCOUNT_BANNED")
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        await _mediator.Send(new LogoutCommand(request));
        return NoContent();
    }
}
