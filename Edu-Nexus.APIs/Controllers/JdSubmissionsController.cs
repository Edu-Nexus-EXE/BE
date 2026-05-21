using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.JdSubmissions.Commands;
using Edu_Nexus.Application.Features.JdSubmissions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("jd-submissions")]
[Authorize]
public class JdSubmissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JdSubmissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitJdRequest request)
    {
        try
        {
            var result = await _mediator.Send(new SubmitJdCommand(request));
            return StatusCode(StatusCodes.Status202Accepted, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "422 ONBOARDING_REQUIRED")
        {
            return UnprocessableEntity(new { error = new { code = "ONBOARDING_REQUIRED", message = "Vui lòng hoàn thành khảo sát trước khi submit JD" } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_SOURCE_TYPE")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_SOURCE_TYPE", message = "sourceType phải là 'url' hoặc 'text'" } });
        }
        catch (Exception ex) when (ex.Message == "422 SOURCE_URL_REQUIRED")
        {
            return UnprocessableEntity(new { error = new { code = "SOURCE_URL_REQUIRED", message = "sourceUrl bắt buộc khi sourceType='url'" } });
        }
        catch (Exception ex) when (ex.Message == "422 RAW_CONTENT_REQUIRED")
        {
            return UnprocessableEntity(new { error = new { code = "RAW_CONTENT_REQUIRED", message = "Vui lòng paste nội dung JD vào rawContent" } });
        }
        catch (Exception ex) when (ex.Message == "422 CONTENT_TOO_LONG")
        {
            return UnprocessableEntity(new { error = new { code = "CONTENT_TOO_LONG", message = "rawContent vượt quá 50000 ký tự" } });
        }
        catch (Exception ex) when (ex.Message.StartsWith("403 QUOTA_EXCEEDED"))
        {
            var parts = ex.Message.Split('|');
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "QUOTA_EXCEEDED",
                    quotaType = parts.Length > 1 ? parts[1] : "jd",
                    current = parts.Length > 2 ? int.Parse(parts[2]) : 0,
                    limit = parts.Length > 3 ? int.Parse(parts[3]) : 0,
                    upgradeUrl = "/pricing"
                }
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        try
        {
            var result = await _mediator.Send(new GetUserJdsQuery(page, pageSize, status));
            return Ok(new { data = result.Data, pagination = result.Pagination });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_STATUS_FILTER")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_STATUS_FILTER", message = "status phải là pending|processing|completed|failed" } });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetJdByIdQuery(id));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "JD không tồn tại hoặc bạn không có quyền truy cập" } });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteJdCommand(id));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "JD không tồn tại hoặc bạn không có quyền truy cập" } });
        }
    }
}
