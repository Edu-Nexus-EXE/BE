namespace Edu_Nexus.Application.DTOs;

public record AdminUserListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string? TierCode,
    string? SubscriptionStatus,
    DateTime? SubscriptionExpiresAt,
    bool IsBanned,
    int JdCount,
    DateTime CreatedAt);

public record AdminPaymentHistoryDto(
    Guid Id,
    decimal Amount,
    string Currency,
    string PaymentProvider,
    string? ProviderOrderId,
    string Status,
    short DurationMonths,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record AdminUserSubscriptionDto(
    string TierCode,
    string DisplayName,
    string Status,
    DateTime StartedAt,
    DateTime? ExpiresAt,
    bool AutoRenew);

public record AdminUserUsageEntry(int Used, int Limit);

public record AdminUserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string? AvatarUrl,
    bool IsBanned,
    bool IsSurveyCompleted,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    AdminUserSubscriptionDto? Subscription,
    Dictionary<string, AdminUserUsageEntry> Usage,
    IReadOnlyList<AdminPaymentHistoryDto> PaymentHistory);

public record SetUserBanRequest(bool IsBanned);
public record ActivateUserSubscriptionRequest(short DurationMonths);
