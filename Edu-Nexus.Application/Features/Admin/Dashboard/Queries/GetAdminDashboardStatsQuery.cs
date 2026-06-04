using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.PaymentOrders;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Dashboard.Queries;

public record GetAdminDashboardStatsQuery() : IRequest<AdminDashboardStatsDto>;

public class GetAdminDashboardStatsQueryHandler : IRequestHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminDashboardStatsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminDashboardStatsDto> Handle(GetAdminDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var allUsers = await _unitOfWork.Users.FindAsync(
            u => u.DeletedAt == null,
            $"{nameof(User.UserSubscription)}.{nameof(UserSubscription.Tier)}",
            cancellationToken);
        var users = allUsers.ToList();

        var totalUsers = users.Count;

        var freeCount = users.Count(u =>
            u.UserSubscription == null
            || u.UserSubscription.Status != UserSubscriptionStatus.Active
            || u.UserSubscription.Tier.TierCode == SubscriptionTierCode.Free);
        var studentCount = users.Count(u =>
            u.UserSubscription != null
            && u.UserSubscription.Status == UserSubscriptionStatus.Active
            && u.UserSubscription.Tier.TierCode == SubscriptionTierCode.Student);

        var totalJd = (await _unitOfWork.JdSubmissions.FindAsync(
            j => j.DeletedAt == null, "", cancellationToken)).Count();

        var revenue = await BuildRevenueAsync(cancellationToken);
        var aiCost = await BuildAiCostAsync(cancellationToken);
        var affiliate = await BuildAffiliateAsync(cancellationToken);

        return new AdminDashboardStatsDto(
            totalUsers,
            new Dictionary<string, int> { ["free"] = freeCount, ["student"] = studentCount },
            totalJd,
            revenue,
            aiCost,
            affiliate);
    }

    private async Task<AdminSubscriptionRevenueDto> BuildRevenueAsync(CancellationToken ct)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var completed = (await _unitOfWork.PaymentOrders.FindAsync(
            o => o.Status == PaymentOrderStatus.Completed,
            nameof(PaymentOrder.User),
            ct)).ToList();

        var allTime = completed.Sum(o => o.Amount);
        var currentMonth = completed.Where(o => o.CompletedAt >= startOfMonth).Sum(o => o.Amount);

        var byProvider = completed
            .GroupBy(o => NormalizeProvider(o.PaymentProvider))
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Amount));

        var currency = completed.FirstOrDefault()?.Currency ?? "VND";

        var recent = completed
            .OrderByDescending(o => o.CompletedAt ?? o.CreatedAt)
            .Take(5)
            .Select(o => new AdminRecentOrderDto(
                o.User?.Email ?? "",
                o.Amount,
                NormalizeProvider(o.PaymentProvider),
                o.Status.ToString().ToLowerInvariant(),
                o.CompletedAt))
            .ToList();

        return new AdminSubscriptionRevenueDto(currentMonth, allTime, currency, byProvider, recent);
    }

    private async Task<AdminAiCostDto> BuildAiCostAsync(CancellationToken ct)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var logs = (await _unitOfWork.RagQueryLogs.GetAllAsync("", ct)).ToList();

        var allTime = logs.Sum(l => l.CostUsd);
        var currentMonth = logs.Where(l => l.CreatedAt >= startOfMonth).Sum(l => l.CostUsd);

        var byPipeline = logs
            .GroupBy(l => l.QueryType)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.CostUsd));

        return new AdminAiCostDto(currentMonth, allTime, byPipeline);
    }

    private async Task<AdminAffiliateStatsDto> BuildAffiliateAsync(CancellationToken ct)
    {
        var clicks = (await _unitOfWork.AffiliateClicks.GetAllAsync("", ct)).ToList();
        var totalClicks = clicks.Count;
        var conversions = clicks.Count(c => c.ConvertedAt != null);
        var revenue = clicks.Where(c => c.ConvertedAt != null && c.CommissionAmount != null)
            .Sum(c => c.CommissionAmount!.Value);

        return new AdminAffiliateStatsDto(totalClicks, conversions, revenue);
    }

    private static string NormalizeProvider(PaymentProvider p) => p switch
    {
        PaymentProvider.ManualTransfer => "manual_transfer",
        _ => p.ToString().ToLowerInvariant()
    };
}
