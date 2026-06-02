using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Features.Portfolios.Commands;
using Edu_Nexus.Application.Features.Portfolios.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
public class PortfoliosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortfoliosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("portfolio")]
    [Authorize]
    public async Task<IActionResult> GetMyPortfolio()
    {
        try
        {
            var result = await _mediator.Send(new GetMyPortfolioQuery());
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
    }

    [HttpPut("portfolio")]
    [Authorize]
    public async Task<IActionResult> UpdateMyPortfolio([FromBody] UpdatePortfolioRequest request)
    {
        try
        {
            var result = await _mediator.Send(new UpdateMyPortfolioCommand(request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
    }

    [HttpGet("p/{slug}")]
    public async Task<IActionResult> GetPublicPortfolio(string slug)
    {
        try
        {
            var result = await _mediator.Send(new GetPublicPortfolioQuery(slug));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Portfolio không tồn tại hoặc đã bị ẩn." } });
        }
    }

    // --- Certificates ---

    [HttpPost("portfolio/certificates")]
    [Authorize]
    public async Task<IActionResult> AddCertificate([FromBody] AddCertificateRequest request)
    {
        try
        {
            var result = await _mediator.Send(new AddCertificateCommand(request));
            return StatusCode(StatusCodes.Status201Created, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message.StartsWith("403 QUOTA_EXCEEDED"))
        {
            var parts = ex.Message.Split('|');
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "QUOTA_EXCEEDED",
                    quotaType = parts.Length > 1 ? parts[1] : "certificate",
                    current = parts.Length > 2 ? int.Parse(parts[2]) : 0,
                    limit = parts.Length > 3 ? int.Parse(parts[3]) : 0,
                    upgradeUrl = "/pricing"
                }
            });
        }
    }

    [HttpPut("portfolio/certificates/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateCertificate(Guid id, [FromBody] AddCertificateRequest request)
    {
        try
        {
            var result = await _mediator.Send(new UpdateCertificateCommand(id, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Không tìm thấy chứng chỉ." } });
        }
    }

    [HttpDelete("portfolio/certificates/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteCertificate(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteCertificateCommand(id));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Không tìm thấy chứng chỉ." } });
        }
    }

    [HttpPatch("portfolio/certificates/{id:guid}/visibility")]
    [Authorize]
    public async Task<IActionResult> ToggleCertificateVisibility(Guid id, [FromBody] ToggleVisibilityRequest request)
    {
        try
        {
            var result = await _mediator.Send(new ToggleCertificateVisibilityCommand(id, request.IsVisible));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Không tìm thấy chứng chỉ." } });
        }
    }

    // --- Projects ---

    [HttpPost("portfolio/projects")]
    [Authorize]
    public async Task<IActionResult> AddProject([FromBody] AddProjectRequest request)
    {
        try
        {
            var result = await _mediator.Send(new AddProjectCommand(request));
            return StatusCode(StatusCodes.Status201Created, new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message.StartsWith("403 QUOTA_EXCEEDED"))
        {
            var parts = ex.Message.Split('|');
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = new
                {
                    code = "QUOTA_EXCEEDED",
                    quotaType = parts.Length > 1 ? parts[1] : "project",
                    current = parts.Length > 2 ? int.Parse(parts[2]) : 0,
                    limit = parts.Length > 3 ? int.Parse(parts[3]) : 0,
                    upgradeUrl = "/pricing"
                }
            });
        }
    }

    [HttpPut("portfolio/projects/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] AddProjectRequest request)
    {
        try
        {
            var result = await _mediator.Send(new UpdateProjectCommand(id, request));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Không tìm thấy dự án." } });
        }
    }

    [HttpDelete("portfolio/projects/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteProjectCommand(id));
            return NoContent();
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Không tìm thấy dự án." } });
        }
    }

    [HttpPatch("portfolio/projects/{id:guid}/visibility")]
    [Authorize]
    public async Task<IActionResult> ToggleProjectVisibility(Guid id, [FromBody] ToggleVisibilityRequest request)
    {
        try
        {
            var result = await _mediator.Send(new ToggleProjectVisibilityCommand(id, request.IsVisible));
            return Ok(new { data = result });
        }
        catch (Exception ex) when (ex.Message == "401 UNAUTHORIZED")
        {
            return Unauthorized();
        }
        catch (Exception ex) when (ex.Message == "404 NOT_FOUND")
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Không tìm thấy dự án." } });
        }
    }
}
