using Edu_Nexus.Application.Features.Admin.Dashboard.Queries;
using Edu_Nexus.Application.Features.Admin.Payments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edu_Nexus.APIs.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
public class AdminPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminPaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("admin/payment-orders")]
    public async Task<IActionResult> ListOrders(
        [FromQuery] string? status,
        [FromQuery] string? provider,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminPaymentOrdersQuery(status, provider, fromDate, toDate, search, page, pageSize));
            return Ok(new
            {
                data = result.Data,
                pagination = result.Pagination,
                summary = result.Summary
            });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_STATUS_FILTER")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_STATUS_FILTER", message = "status phải là pending|completed|failed|cancelled" } });
        }
        catch (Exception ex) when (ex.Message == "422 INVALID_PROVIDER_FILTER")
        {
            return UnprocessableEntity(new { error = new { code = "INVALID_PROVIDER_FILTER", message = "provider phải là vnpay|momo|manual_transfer|sepay" } });
        }
    }

    [HttpGet("admin/dashboard/stats")]
    public async Task<IActionResult> DashboardStats()
    {
        var result = await _mediator.Send(new GetAdminDashboardStatsQuery());
        return Ok(new { data = result });
    }
}
