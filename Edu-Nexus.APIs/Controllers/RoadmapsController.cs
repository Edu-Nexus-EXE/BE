using Edu_Nexus.Application.Features.Roadmaps.Commands;
using Edu_Nexus.Application.Features.Roadmaps.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Authorize]
public class RoadmapsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoadmapsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("jd-submissions/{jdId:guid}/roadmaps")]
    public async Task<IActionResult> GenerateRoadmap(Guid jdId)
    {
        try
        {
            var result = await _mediator.Send(new GenerateRoadmapCommand(jdId));
            return StatusCode(StatusCodes.Status202Accepted, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "422 GAP_ANALYSIS_NOT_COMPLETED")
        {
            return UnprocessableEntity(new { error = new { code = "GAP_ANALYSIS_NOT_COMPLETED", message = "Cần hoàn thành Gap Analysis trước khi tạo Roadmap." } });
        }
        catch (Exception ex) when (ex.Message.StartsWith("403 QUOTA_EXCEEDED"))
        {
            var parts = ex.Message.Split('|');
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "QUOTA_EXCEEDED",
                    quotaType = parts.Length > 1 ? parts[1] : "roadmapActive",
                    current = parts.Length > 2 ? int.Parse(parts[2]) : 0,
                    limit = parts.Length > 3 ? int.Parse(parts[3]) : 0,
                    upgradeUrl = "/pricing"
                }
            });
        }
    }

    [HttpGet("roadmaps/{id:guid}")]
    public async Task<IActionResult> GetRoadmap(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetRoadmapQuery(id));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Roadmap không tồn tại hoặc bạn không có quyền truy cập." } });
        }
    }

    [HttpGet("users/me/roadmaps")]
    public async Task<IActionResult> GetMyRoadmaps([FromQuery] string? status = null)
    {
        try
        {
            var result = await _mediator.Send(new GetMyRoadmapsQuery(status));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
    }

    [HttpPatch("roadmap-nodes/{nodeId:guid}/status")]
    public async Task<IActionResult> UpdateNodeStatus(Guid nodeId, [FromBody] UpdateRoadmapNodeStatusCommand command)
    {
        if (nodeId != command.NodeId)
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "NodeId in URL and Body must match." } });
        }

        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Node không tồn tại hoặc bạn không có quyền truy cập." } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_STATUS")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_STATUS", message = "Trạng thái không hợp lệ." } });
        }
    }

    [HttpPatch("roadmaps/{id:guid}/archive")]
    public async Task<IActionResult> ArchiveRoadmap(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new ArchiveRoadmapCommand(id));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Roadmap không tồn tại hoặc bạn không có quyền truy cập." } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_STATUS")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_STATUS", message = "Chỉ có thể archive roadmap đang ở trạng thái Active." } });
        }
    }

    [HttpPost("roadmaps/{id:guid}/regenerate")]
    public async Task<IActionResult> RegenerateRoadmap(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new RegenerateRoadmapCommand(id));
            return StatusCode(StatusCodes.Status202Accepted, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Roadmap không tồn tại hoặc bạn không có quyền truy cập." } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_STATUS")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_STATUS", message = "Không thể tạo lại Roadmap hiện tại." } });
        }
        catch (Exception ex) when (ex.Message == "422 GAP_ANALYSIS_NOT_COMPLETED")
        {
            return UnprocessableEntity(new { error = new { code = "GAP_ANALYSIS_NOT_COMPLETED", message = "Cần hoàn thành Gap Analysis." } });
        }
    }

    [HttpPatch("roadmaps/{id:guid}/keep")]
    public async Task<IActionResult> KeepRoadmap(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new KeepRoadmapCommand(id));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Roadmap không tồn tại hoặc bạn không có quyền truy cập." } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_STATUS")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_STATUS", message = "Roadmap này không ở trạng thái Outdated." } });
        }
    }
}
