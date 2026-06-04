using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Admin.Skills.Commands;
using Edu_Nexus.Application.Features.Admin.Skills.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("admin/skills")]
[Authorize(Roles = "admin")]
public class AdminSkillsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminSkillsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetSkills([FromQuery] string? search, [FromQuery] string? major, [FromQuery] string? category, [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetSkillsQuery(search, major, category, isActive, page, pageSize));
        return Ok(result);
    }

    [HttpGet("pending-review")]
    public async Task<IActionResult> GetPendingReviewSkills([FromQuery] string? search, [FromQuery] string? major, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPendingReviewSkillsQuery(search, major, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSkill([FromBody] CreateSkillRequest request)
    {
        try
        {
            var result = await _mediator.Send(new CreateSkillCommand(request));
            return StatusCode(StatusCodes.Status201Created, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "409 SLUG_TAKEN")
        {
            return Conflict(new { error = new { code = "SLUG_TAKEN", message = "Slug đã tồn tại." } });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSkill(Guid id, [FromBody] UpdateSkillRequest request)
    {
        try
        {
            var result = await _mediator.Send(new UpdateSkillCommand(id, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "404 SKILL_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SKILL_NOT_FOUND", message = "Không tìm thấy skill." } });
        }
        catch (Exception ex) when (ex.Message == "409 SLUG_TAKEN")
        {
            return Conflict(new { error = new { code = "SLUG_TAKEN", message = "Slug đã tồn tại." } });
        }
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> ToggleSkillActive(Guid id, [FromBody] ToggleSkillActiveRequest request)
    {
        try
        {
            var result = await _mediator.Send(new ToggleSkillActiveCommand(id, request.IsActive));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "404 SKILL_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SKILL_NOT_FOUND", message = "Không tìm thấy skill." } });
        }
    }

    [HttpPost("{id:guid}/prerequisites")]
    public async Task<IActionResult> AddPrerequisite(Guid id, [FromBody] AddPrerequisiteRequest request)
    {
        try
        {
            await _mediator.Send(new AddSkillPrerequisiteCommand(id, request));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "404 SKILL_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SKILL_NOT_FOUND", message = "Không tìm thấy skill." } });
        }
        catch (Exception ex) when (ex.Message == "404 PREREQUISITE_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "PREREQUISITE_NOT_FOUND", message = "Không tìm thấy prerequisite skill." } });
        }
        catch (Exception ex) when (ex.Message == "422 CYCLE_DETECTED")
        {
            return UnprocessableEntity(new { error = new { code = "CYCLE_DETECTED", message = "Phát hiện vòng lặp prerequisite." } });
        }
    }

    [HttpDelete("{id:guid}/prerequisites/{prereqId:guid}")]
    public async Task<IActionResult> RemovePrerequisite(Guid id, Guid prereqId)
    {
        try
        {
            await _mediator.Send(new RemoveSkillPrerequisiteCommand(id, prereqId));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "404 SKILL_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SKILL_NOT_FOUND", message = "Không tìm thấy skill." } });
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveSkill(Guid id, [FromBody] ApproveSkillRequest request)
    {
        try
        {
            var result = await _mediator.Send(new ApproveSkillCommand(id, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "404 SKILL_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SKILL_NOT_FOUND", message = "Không tìm thấy skill." } });
        }
        catch (Exception ex) when (ex.Message == "409 NOT_PENDING_REVIEW")
        {
            return Conflict(new { error = new { code = "NOT_PENDING_REVIEW", message = "Skill này không nằm trong hàng đợi duyệt." } });
        }
    }

    [HttpPost("{oldId:guid}/merge-to/{newId:guid}")]
    public async Task<IActionResult> MergeSkill(Guid oldId, Guid newId, [FromBody] MergeSkillRequest request)
    {
        try
        {
            var result = await _mediator.Send(new MergeSkillCommand(oldId, newId, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "404 SKILL_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SKILL_NOT_FOUND", message = "Không tìm thấy skill." } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_MERGE")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_MERGE", message = "Không thể merge cùng 1 skill." } });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
    }
}
