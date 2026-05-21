using Edu_Nexus.Application.Features.CvSubmissions.Commands;
using Edu_Nexus.Application.Features.CvSubmissions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("assessment-paths/{pathId:guid}/cv")]
[Authorize]
public class CvSubmissionsController : ControllerBase
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private readonly IMediator _mediator;

    public CvSubmissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Upload(Guid pathId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return UnprocessableEntity(new { error = new { code = "EMPTY_FILE", message = "Vui lòng chọn file PDF" } });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _mediator.Send(new UploadCvCommand(
                pathId,
                stream,
                file.FileName,
                file.Length,
                file.ContentType ?? "application/octet-stream"));

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
            return UnprocessableEntity(new { error = new { code = "PATH_TYPE_MISMATCH", message = "Assessment path này không phải Path A (CV)" } });
        }
        catch (Exception ex) when (ex.Message == "422 EMPTY_FILE")
        {
            return UnprocessableEntity(new { error = new { code = "EMPTY_FILE", message = "File rỗng" } });
        }
        catch (Exception ex) when (ex.Message == "422 FILE_TOO_LARGE")
        {
            return UnprocessableEntity(new { error = new { code = "FILE_TOO_LARGE", message = "File vượt quá 5MB" } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_FILE_TYPE")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_FILE_TYPE", message = "Chỉ chấp nhận file PDF" } });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid pathId)
    {
        try
        {
            var result = await _mediator.Send(new GetCvByPathQuery(pathId));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 CV_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "CV_NOT_FOUND", message = "Chưa có CV cho assessment path này" } });
        }
    }
}
