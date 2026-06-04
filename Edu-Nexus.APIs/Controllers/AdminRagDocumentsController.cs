using Edu_Nexus.Application.Features.Admin.RagDocuments.Commands;
using Edu_Nexus.Application.Features.Admin.RagDocuments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("admin/rag-documents")]
[Authorize(Roles = "admin")]
public class AdminRagDocumentsController : ControllerBase
{
    private const long MaxUploadBytes = 50 * 1024 * 1024;

    private readonly IMediator _mediator;

    public AdminRagDocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetAdminRagDocumentsQuery(page, pageSize));
        return Ok(new { data = result.Data, pagination = result.Pagination });
    }

    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string title,
        [FromForm] string sourceType,
        [FromForm] List<Guid>? relatedSkillIds)
    {
        if (file == null || file.Length == 0)
        {
            return UnprocessableEntity(new { error = new { code = "EMPTY_FILE", message = "Vui lòng chọn file PDF." } });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _mediator.Send(new UploadRagDocumentCommand(
                stream,
                file.FileName,
                file.Length,
                file.ContentType ?? "application/octet-stream",
                title,
                sourceType,
                relatedSkillIds ?? new List<Guid>()));
            return StatusCode(StatusCodes.Status202Accepted, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED") { return Unauthorized(); }
        catch (Exception ex) when (MapError(ex.Message) is { } payload) { return UnprocessableEntity(payload); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteRagDocumentCommand(id));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED") { return Unauthorized(); }
        catch (Exception ex) when (ex.Message == "404 DOCUMENT_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "DOCUMENT_NOT_FOUND", message = "Không tìm thấy tài liệu RAG." } });
        }
    }

    private static object? MapError(string msg) => msg switch
    {
        "422 TITLE_REQUIRED" => new { error = new { code = "TITLE_REQUIRED", message = "title bắt buộc." } },
        "422 EMPTY_FILE" => new { error = new { code = "EMPTY_FILE", message = "File rỗng." } },
        "422 FILE_TOO_LARGE" => new { error = new { code = "FILE_TOO_LARGE", message = "File vượt quá 50MB." } },
        "422 INVALID_FILE_TYPE" => new { error = new { code = "INVALID_FILE_TYPE", message = "Chỉ chấp nhận file PDF." } },
        "422 INVALID_SOURCE_TYPE" => new { error = new { code = "INVALID_SOURCE_TYPE", message = "sourceType phải là fptu_curriculum|fptu_syllabus|external_doc." } },
        _ => null
    };
}
