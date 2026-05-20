using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Onboarding.Commands;
using Edu_Nexus.Application.Features.Onboarding.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IMediator _mediator;

    public OnboardingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetOnboarding()
    {
        try
        {
            var result = await _mediator.Send(new GetOnboardingQuery());
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitOnboarding([FromBody] SubmitOnboardingRequest request)
    {
        try
        {
            var result = await _mediator.Send(new SubmitOnboardingCommand(request));
            return StatusCode(StatusCodes.Status201Created, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "409 ALREADY_COMPLETED")
        {
            return Conflict(new { error = new { code = "ALREADY_COMPLETED", message = "Onboarding survey already completed" } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_DATA")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_DATA", message = "One or more fields do not match the allowed values" } });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateOnboarding([FromBody] SubmitOnboardingRequest request)
    {
        try
        {
            var result = await _mediator.Send(new UpdateOnboardingCommand(request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Onboarding survey not found" } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_DATA")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_DATA", message = "One or more fields do not match the allowed values" } });
        }
    }
}
