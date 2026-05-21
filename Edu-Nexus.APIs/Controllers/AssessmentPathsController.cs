using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.AssessmentPaths.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("jd-submissions/{jdId:guid}/assessment-path")]
[Authorize]
public class AssessmentPathsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssessmentPathsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid jdId, [FromBody] CreateAssessmentPathRequest request)
    {
        try
        {
            var result = await _mediator.Send(new CreateAssessmentPathCommand(jdId, request));
            return StatusCode(StatusCodes.Status201Created, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 JD_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "JD_NOT_FOUND", message = "JD không tồn tại hoặc bạn không có quyền truy cập" } });
        }
        catch (Exception ex) when (ex.Message == "409 PATH_ALREADY_EXISTS")
        {
            return Conflict(new { error = new { code = "PATH_ALREADY_EXISTS", message = "JD này đã có assessment path. Gọi DELETE trước nếu muốn đổi." } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_PATH_TYPE")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_PATH_TYPE", message = "pathType phải là 'cv' hoặc 'assessment'" } });
        }
        catch (Exception ex) when (ex.Message.StartsWith("403 QUOTA_EXCEEDED"))
        {
            var parts = ex.Message.Split('|');
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "QUOTA_EXCEEDED",
                    quotaType = parts.Length > 1 ? parts[1] : "assessment",
                    current = parts.Length > 2 ? int.Parse(parts[2]) : 0,
                    limit = parts.Length > 3 ? int.Parse(parts[3]) : 0,
                    upgradeUrl = "/pricing"
                }
            });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid jdId)
    {
        try
        {
            await _mediator.Send(new DeleteAssessmentPathCommand(jdId));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 PATH_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "PATH_NOT_FOUND", message = "Assessment path chưa được tạo cho JD này" } });
        }
        catch (Exception ex) when (ex.Message == "422 CANNOT_RESET_AFTER_GAP")
        {
            return UnprocessableEntity(new { error = new { code = "CANNOT_RESET_AFTER_GAP", message = "Không thể reset path sau khi Gap Analysis đã chạy. Hãy xóa Gap Analysis trước." } });
        }
    }
}
