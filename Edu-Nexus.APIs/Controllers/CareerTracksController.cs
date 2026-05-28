using Edu_Nexus.Application.Features.CareerTracks.Commands;
using Edu_Nexus.Application.Features.CareerTracks.DTOs;
using Edu_Nexus.Application.Features.CareerTracks.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("career-tracks")]
[Authorize]
public class CareerTracksController : ControllerBase
{
    private readonly IMediator _mediator;

    public CareerTracksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCareerTrack([FromBody] CreateCareerTrackCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message.StartsWith("403 QUOTA_EXCEEDED"))
        {
            var parts = ex.Message.Split('|');
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "QUOTA_EXCEEDED",
                    quotaType = parts.Length > 1 ? parts[1] : "careerTrack",
                    current = parts.Length > 2 ? int.Parse(parts[2]) : 0,
                    limit = parts.Length > 3 ? int.Parse(parts[3]) : 0,
                    upgradeUrl = "/pricing"
                }
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCareerTracks()
    {
        try
        {
            var result = await _mediator.Send(new GetCareerTracksQuery());
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCareerTrackById(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetCareerTrackByIdQuery { Id = id });
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Career Track không tồn tại hoặc bạn không có quyền truy cập." } });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCareerTrack(Guid id, [FromBody] UpdateCareerTrackCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "Id in URL must match Id in body." } });
        }
        
        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Career Track không tồn tại hoặc bạn không có quyền truy cập." } });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCareerTrack(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteCareerTrackCommand { Id = id });
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Career Track không tồn tại hoặc bạn không có quyền truy cập." } });
        }
    }

    [HttpPost("{id}/jds")]
    public async Task<IActionResult> AddJdToCareerTrack(Guid id, [FromBody] AddJdToCareerTrackCommand command)
    {
        if (id != command.CareerTrackId)
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "CareerTrackId in URL must match CareerTrackId in body." } });
        }

        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Career Track hoặc JD không tồn tại." } });
        }
        catch (Exception ex) when (ex.Message == "409 CONFLICT")
        {
            return Conflict(new { error = new { code = "CONFLICT", message = "JD đã tồn tại trong Career Track." } });
        }
    }

    [HttpDelete("{id}/jds/{jdId}")]
    public async Task<IActionResult> RemoveJdFromCareerTrack(Guid id, Guid jdId)
    {
        try
        {
            await _mediator.Send(new RemoveJdFromCareerTrackCommand { CareerTrackId = id, JdId = jdId });
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Career Track hoặc liên kết JD không tồn tại." } });
        }
    }
}
