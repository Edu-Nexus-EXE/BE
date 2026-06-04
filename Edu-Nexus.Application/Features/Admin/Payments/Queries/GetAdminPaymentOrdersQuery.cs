using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.PaymentOrders;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Payments.Queries;

public record GetAdminPaymentOrdersQuery(
    string? Status,
    string? Provider,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    int Page,
    int PageSize) : IRequest<AdminPaymentOrdersResultDto>;

public class GetAdminPaymentOrdersQueryHandler : IRequestHandler<GetAdminPaymentOrdersQuery, AdminPaymentOrdersResultDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminPaymentOrdersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminPaymentOrdersResultDto> Handle(GetAdminPaymentOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        PaymentOrderStatus? statusFilter = NormalizeStatus(request.Status);
        PaymentProvider? providerFilter = NormalizeProvider(request.Provider);
        var from = request.FromDate;
        var to = request.ToDate;
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        var rows = (await _unitOfWork.PaymentOrders.FindAsync(
            o => (statusFilter == null || o.Status == statusFilter)
                 && (providerFilter == null || o.PaymentProvider == providerFilter)
                 && (from == null || o.CreatedAt >= from)
                 && (to == null || o.CreatedAt <= to)
                 && (search == null || (o.User != null && (o.User.Email.Contains(search) || o.User.FullName.Contains(search)))),
            $"{nameof(PaymentOrder.User)},{nameof(PaymentOrder.Tier)}",
            cancellationToken))
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var totalItems = rows.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AdminPaymentOrderItemDto(
                o.Id,
                o.UserId,
                o.User?.Email ?? "",
                o.User?.FullName ?? "",
                o.Tier?.TierCode.ToString().ToLowerInvariant() ?? "",
                o.Amount,
                o.Currency,
                NormalizeProviderLabel(o.PaymentProvider),
                o.ProviderOrderId,
                o.Status.ToString().ToLowerInvariant(),
                o.DurationMonths,
                o.CreatedAt,
                o.CompletedAt))
            .ToList();

        var completed = rows.Where(o => o.Status == PaymentOrderStatus.Completed).ToList();
        var totalRevenue = completed.Sum(o => o.Amount);
        var summaryCurrency = completed.FirstOrDefault()?.Currency ?? "VND";

        return new AdminPaymentOrdersResultDto(
            items,
            new PaginationDto(page, pageSize, totalItems, totalPages),
            new AdminPaymentSummaryDto(completed.Count, totalRevenue, summaryCurrency));
    }

    private static PaymentOrderStatus? NormalizeStatus(string? raw) => raw?.ToLowerInvariant() switch
    {
        "pending" => PaymentOrderStatus.Pending,
        "completed" => PaymentOrderStatus.Completed,
        "failed" => PaymentOrderStatus.Failed,
        "cancelled" => PaymentOrderStatus.Cancelled,
        null or "" => null,
        _ => throw new Exception("422 INVALID_STATUS_FILTER")
    };

    private static PaymentProvider? NormalizeProvider(string? raw) => raw?.ToLowerInvariant() switch
    {
        "vnpay" => PaymentProvider.Vnpay,
        "momo" => PaymentProvider.Momo,
        "manual_transfer" => PaymentProvider.ManualTransfer,
        "sepay" => Enum.TryParse<PaymentProvider>("Sepay", true, out var p) ? p : null,
        null or "" => null,
        _ => throw new Exception("422 INVALID_PROVIDER_FILTER")
    };

    private static string NormalizeProviderLabel(PaymentProvider p) => p switch
    {
        PaymentProvider.ManualTransfer => "manual_transfer",
        _ => p.ToString().ToLowerInvariant()
    };
}
