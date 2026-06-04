using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Admin.JdSubmissions.Commands;
using Edu_Nexus.Application.Features.Admin.JdSubmissions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("admin/jd-submissions")]
[Authorize(Roles = "admin")]
public class AdminJdsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminJdsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery(Name = "parseStatus")] string? parseStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminJdsQuery(parseStatus, page, pageSize));
            return Ok(new { data = result.Data, pagination = result.Pagination });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_STATUS_FILTER")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_STATUS_FILTER", message = "parseStatus phải là pending|processing|completed|failed" } });
        }
    }

    [HttpPost("{id:guid}/re-parse")]
    public async Task<IActionResult> ReParse(Guid id)
    {
        try
        {
            await _mediator.Send(new ReParseJdCommand(id));
            return Accepted();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 JD_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "JD_NOT_FOUND", message = "Không tìm thấy JD." } });
        }
        catch (Exception ex) when (ex.Message == "422 ALREADY_PROCESSING")
        {
            return UnprocessableEntity(new { error = new { code = "ALREADY_PROCESSING", message = "JD đang processing — chờ job hiện tại xong trước." } });
        }
    }

    [HttpPatch("{id:guid}/mark-invalid")]
    public async Task<IActionResult> MarkInvalid(Guid id, [FromBody] MarkJdInvalidRequest? request)
    {
        try
        {
            await _mediator.Send(new MarkJdInvalidCommand(id, request));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 JD_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "JD_NOT_FOUND", message = "Không tìm thấy JD." } });
        }
    }
}
