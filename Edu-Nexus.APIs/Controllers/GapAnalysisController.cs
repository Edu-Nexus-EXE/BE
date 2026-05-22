using Edu_Nexus.Application.Features.GapAnalyses.Commands;
using Edu_Nexus.Application.Features.GapAnalyses.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("jd-submissions/{jdId:guid}/gap-analysis")]
[Authorize]
public class GapAnalysisController : ControllerBase
{
    private readonly IMediator _mediator;

    public GapAnalysisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Start(Guid jdId)
    {
        try
        {
            var result = await _mediator.Send(new StartGapAnalysisCommand(jdId));
            return StatusCode(StatusCodes.Status202Accepted, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 JD_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "JD_NOT_FOUND", message = "JD không tồn tại hoặc bạn không có quyền truy cập" } });
        }
        catch (Exception ex) when (ex.Message == "422 JD_NOT_PARSED")
        {
            return UnprocessableEntity(new { error = new { code = "JD_NOT_PARSED", message = "JD chưa parse xong, vui lòng đợi" } });
        }
        catch (Exception ex) when (ex.Message == "422 ASSESSMENT_PATH_REQUIRED")
        {
            return UnprocessableEntity(new { error = new { code = "ASSESSMENT_PATH_REQUIRED", message = "Cần chọn assessment path (CV hoặc Assessment) trước khi chạy gap analysis" } });
        }
        catch (Exception ex) when (ex.Message == "422 CV_NOT_READY")
        {
            return UnprocessableEntity(new { error = new { code = "CV_NOT_READY", message = "CV chưa được upload hoặc chưa parse xong" } });
        }
        catch (Exception ex) when (ex.Message == "422 ASSESSMENT_NOT_SUBMITTED")
        {
            return UnprocessableEntity(new { error = new { code = "ASSESSMENT_NOT_SUBMITTED", message = "Chưa submit assessment session nào" } });
        }
        catch (Exception ex) when (ex.Message.StartsWith("403 QUOTA_EXCEEDED"))
        {
            var parts = ex.Message.Split('|');
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "QUOTA_EXCEEDED",
                    quotaType = parts.Length > 1 ? parts[1] : "gapAnalysis",
                    current = parts.Length > 2 ? int.Parse(parts[2]) : 0,
                    limit = parts.Length > 3 ? int.Parse(parts[3]) : 0,
                    upgradeUrl = "/pricing"
                }
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid jdId, [FromQuery] bool all = false)
    {
        try
        {
            var result = await _mediator.Send(new GetGapAnalysisQuery(jdId, all));
            if (all)
            {
                return Ok(new { data = result });
            }
            return Ok(new { data = result.FirstOrDefault() });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 JD_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "JD_NOT_FOUND", message = "JD không tồn tại hoặc bạn không có quyền truy cập" } });
        }
        catch (Exception ex) when (ex.Message == "404 GAP_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "GAP_NOT_FOUND", message = "Chưa có gap analysis cho JD này" } });
        }
    }
}
