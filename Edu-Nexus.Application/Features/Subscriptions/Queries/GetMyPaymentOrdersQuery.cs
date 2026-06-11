using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.PaymentOrders;
using MediatR;

namespace Edu_Nexus.Application.Features.Subscriptions.Queries;

public record GetMyPaymentOrdersQuery(int Page, int PageSize, string? Status)
    : IRequest<PagedResult<MyPaymentOrderItemDto>>;

public class GetMyPaymentOrdersQueryHandler : IRequestHandler<GetMyPaymentOrdersQuery, PagedResult<MyPaymentOrderItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyPaymentOrdersQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<MyPaymentOrderItemDto>> Handle(GetMyPaymentOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        PaymentOrderStatus? statusFilter = request.Status?.ToLowerInvariant() switch
        {
            "pending" => PaymentOrderStatus.Pending,
            "completed" => PaymentOrderStatus.Completed,
            "failed" => PaymentOrderStatus.Failed,
            "cancelled" => PaymentOrderStatus.Cancelled,
            null or "" => null,
            _ => throw new Exception("422 INVALID_STATUS_FILTER")
        };

        var rows = (await _unitOfWork.PaymentOrders.FindAsync(
            o => o.UserId == userId && (statusFilter == null || o.Status == statusFilter),
            nameof(PaymentOrder.Tier),
            cancellationToken))
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var totalItems = rows.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new MyPaymentOrderItemDto(
                o.Id,
                o.Tier?.TierCode.ToString().ToLowerInvariant() ?? "",
                o.Amount,
                o.Currency,
                o.PaymentProvider == PaymentProvider.ManualTransfer
                    ? "manual_transfer"
                    : o.PaymentProvider.ToString().ToLowerInvariant(),
                o.ProviderOrderId,
                o.Status.ToString().ToLowerInvariant(),
                o.DurationMonths,
                o.CreatedAt,
                o.CompletedAt))
            .ToList();

        return new PagedResult<MyPaymentOrderItemDto>(
            items,
            new PaginationDto(page, pageSize, totalItems, totalPages));
    }
}
