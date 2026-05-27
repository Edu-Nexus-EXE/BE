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
[Route("api/career-tracks")]
[Authorize]
public class CareerTracksController : ControllerBase
{
    private readonly IMediator _mediator;

    public CareerTracksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<CareerTrackDto>> CreateCareerTrack([FromBody] CreateCareerTrackCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<CareerTrackDto>>> GetCareerTracks()
    {
        var result = await _mediator.Send(new GetCareerTracksQuery());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CareerTrackDetailDto>> GetCareerTrackById(Guid id)
    {
        var result = await _mediator.Send(new GetCareerTrackByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCareerTrack(Guid id, [FromBody] UpdateCareerTrackCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("Id in URL must match Id in body.");
        }
        
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCareerTrack(Guid id)
    {
        await _mediator.Send(new DeleteCareerTrackCommand { Id = id });
        return NoContent();
    }

    [HttpPost("{id}/jds")]
    public async Task<ActionResult> AddJdToCareerTrack(Guid id, [FromBody] AddJdToCareerTrackCommand command)
    {
        if (id != command.CareerTrackId)
        {
            return BadRequest("CareerTrackId in URL must match CareerTrackId in body.");
        }

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}/jds/{jdId}")]
    public async Task<ActionResult> RemoveJdFromCareerTrack(Guid id, Guid jdId)
    {
        await _mediator.Send(new RemoveJdFromCareerTrackCommand { CareerTrackId = id, JdId = jdId });
        return NoContent();
    }
}
