namespace Edu_Nexus.Application.DTOs;

public record AdminPaymentOrderItemDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    string TierCode,
    decimal Amount,
    string Currency,
    string PaymentProvider,
    string? ProviderOrderId,
    string Status,
    short DurationMonths,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record AdminPaymentSummaryDto(int TotalCompleted, decimal TotalRevenue, string Currency);

public record AdminPaymentOrdersResultDto(
    IReadOnlyList<AdminPaymentOrderItemDto> Data,
    PaginationDto Pagination,
    AdminPaymentSummaryDto Summary);

public record AdminRecentOrderDto(string UserEmail, decimal Amount, string Provider, string Status, DateTime? CompletedAt);

public record AdminSubscriptionRevenueDto(
    decimal CurrentMonth,
    decimal AllTime,
    string Currency,
    Dictionary<string, decimal> ByProvider,
    IReadOnlyList<AdminRecentOrderDto> RecentOrders);

public record AdminAiCostDto(decimal CurrentMonth, decimal AllTime, Dictionary<string, decimal> ByPipeline);

public record AdminAffiliateStatsDto(int TotalClicks, int TotalConversions, decimal EstimatedRevenue);

public record AdminDashboardStatsDto(
    int TotalUsers,
    Dictionary<string, int> UsersByTier,
    int TotalJdSubmitted,
    AdminSubscriptionRevenueDto SubscriptionRevenue,
    AdminAiCostDto TotalAiCost,
    AdminAffiliateStatsDto AffiliateStats);
