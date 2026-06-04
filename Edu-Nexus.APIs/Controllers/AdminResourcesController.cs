using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Admin.Resources.Commands;
using Edu_Nexus.Application.Features.Admin.Resources.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Route("admin/resources")]
[Authorize(Roles = "admin")]
public class AdminResourcesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminResourcesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? type,
        [FromQuery] string? search,
        [FromQuery(Name = "needsReview")] bool? needsReview,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminResourcesQuery(type, search, needsReview, isActive, page, pageSize));
            return Ok(new { data = result.Data, pagination = result.Pagination });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_RESOURCE_TYPE")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_RESOURCE_TYPE", message = "type không hợp lệ." } });
        }
    }

    [HttpGet("pending-review")]
    public Task<IActionResult> PendingReview([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => List(null, null, true, true, page, pageSize);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminResourceUpsertRequest request)
    {
        try
        {
            var result = await _mediator.Send(new CreateResourceCommand(request));
            return StatusCode(StatusCodes.Status201Created, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED") { return Unauthorized(); }
        catch (Exception ex) when (MapValidationError(ex.Message) is { } payload) { return UnprocessableEntity(payload); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminResourceUpsertRequest request)
    {
        try
        {
            var result = await _mediator.Send(new UpdateResourceCommand(id, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED") { return Unauthorized(); }
        catch (Exception ex) when (ex.Message == "404 RESOURCE_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "RESOURCE_NOT_FOUND", message = "Không tìm thấy resource." } });
        }
        catch (Exception ex) when (MapValidationError(ex.Message) is { } payload) { return UnprocessableEntity(payload); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteResourceCommand(id));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED") { return Unauthorized(); }
        catch (Exception ex) when (ex.Message == "404 RESOURCE_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "RESOURCE_NOT_FOUND", message = "Không tìm thấy resource." } });
        }
    }

    [HttpPatch("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, [FromBody] AdminResourceReviewRequest request)
    {
        try
        {
            var result = await _mediator.Send(new ReviewResourceCommand(id, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED") { return Unauthorized(); }
        catch (Exception ex) when (ex.Message == "404 RESOURCE_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "RESOURCE_NOT_FOUND", message = "Không tìm thấy resource." } });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id)
    {
        try
        {
            await _mediator.Send(new RejectResourceCommand(id));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED") { return Unauthorized(); }
        catch (Exception ex) when (ex.Message == "404 RESOURCE_NOT_FOUND")
        {
            return NotFound(new { error = new { code = "RESOURCE_NOT_FOUND", message = "Không tìm thấy resource." } });
        }
    }

    private static object? MapValidationError(string msg) => msg switch
    {
        "422 TITLE_REQUIRED" => new { error = new { code = "TITLE_REQUIRED", message = "title bắt buộc." } },
        "422 URL_REQUIRED" => new { error = new { code = "URL_REQUIRED", message = "url bắt buộc." } },
        "422 LANGUAGE_REQUIRED" => new { error = new { code = "LANGUAGE_REQUIRED", message = "language bắt buộc." } },
        "422 INVALID_RESOURCE_TYPE" => new { error = new { code = "INVALID_RESOURCE_TYPE", message = "type không hợp lệ." } },
        "422 INVALID_ACCESS_TYPE" => new { error = new { code = "INVALID_ACCESS_TYPE", message = "accessType không hợp lệ." } },
        _ => null
    };
}
