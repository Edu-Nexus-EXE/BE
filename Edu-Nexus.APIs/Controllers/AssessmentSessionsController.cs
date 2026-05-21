using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.AssessmentSessions.Commands;
using Edu_Nexus.Application.Features.AssessmentSessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Authorize]
public class AssessmentSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssessmentSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("assessment-paths/{pathId:guid}/sessions")]
    public async Task<IActionResult> Start(Guid pathId, [FromBody] StartAssessmentSessionRequest request)
    {
        try
        {
            var result = await _mediator.Send(new StartAssessmentSessionCommand(pathId, request));
            return StatusCode(StatusCodes.Status202Accepted, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 PATH_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "PATH_NOT_FOUND", message = "Assessment path không tồn tại hoặc bạn không có quyền truy cập" } });
        }
        catch (Exception ex) when (ex.Message == "422 PATH_TYPE_MISMATCH")
        {
            return UnprocessableEntity(new { error = new { code = "PATH_TYPE_MISMATCH", message = "Assessment path này không phải Path B (Assessment)" } });
        }
        catch (Exception ex) when (ex.Message == "404 REUSE_SESSION_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "REUSE_SESSION_NOT_FOUND", message = "Session cần reuse không tồn tại hoặc không khớp job_role_category" } });
        }
    }

    [HttpGet("assessment-sessions/{sessionId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid sessionId)
    {
        try
        {
            var result = await _mediator.Send(new GetSessionQuestionsQuery(sessionId));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 SESSION_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SESSION_NOT_FOUND", message = "Session không tồn tại hoặc bạn không có quyền truy cập" } });
        }
    }

    [HttpPost("assessment-sessions/{sessionId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid sessionId, [FromBody] SubmitAssessmentSessionRequest request)
    {
        try
        {
            var result = await _mediator.Send(new SubmitAssessmentSessionCommand(sessionId, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 SESSION_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SESSION_NOT_FOUND", message = "Session không tồn tại hoặc bạn không có quyền truy cập" } });
        }
        catch (Exception ex) when (ex.Message == "409 ALREADY_SUBMITTED")
        {
            return Conflict(new { error = new { code = "ALREADY_SUBMITTED", message = "Session này đã submit" } });
        }
        catch (Exception ex) when (ex.Message == "422 QUESTIONS_NOT_READY")
        {
            return UnprocessableEntity(new { error = new { code = "QUESTIONS_NOT_READY", message = "Câu hỏi chưa được sinh xong. Vui lòng đợi." } });
        }
        catch (Exception ex) when (ex.Message == "422 ANSWER_COUNT_MISMATCH")
        {
            return UnprocessableEntity(new { error = new { code = "ANSWER_COUNT_MISMATCH", message = "Số câu trả lời không khớp số câu hỏi" } });
        }
        catch (Exception ex) when (ex.Message == "422 MISSING_ANSWER")
        {
            return UnprocessableEntity(new { error = new { code = "MISSING_ANSWER", message = "Thiếu câu trả lời cho ít nhất 1 câu hỏi" } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_OPTION")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_OPTION", message = "selectedOption phải là A/B/C/D" } });
        }
    }

    [HttpGet("assessment-sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetResult(Guid sessionId)
    {
        try
        {
            var result = await _mediator.Send(new GetSessionResultQuery(sessionId));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 SESSION_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "SESSION_NOT_FOUND", message = "Session không tồn tại hoặc bạn không có quyền truy cập" } });
        }
    }

    [HttpGet("jd-submissions/{jdId:guid}/reusable-sessions")]
    public async Task<IActionResult> GetReusable(Guid jdId)
    {
        try
        {
            var result = await _mediator.Send(new GetReusableSessionsQuery(jdId));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 JD_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "JD_NOT_FOUND", message = "JD không tồn tại hoặc bạn không có quyền truy cập" } });
        }
    }
}
