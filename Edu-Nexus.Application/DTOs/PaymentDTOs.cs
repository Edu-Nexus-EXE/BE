namespace Edu_Nexus.Application.DTOs;

// ─── Tiers ───────────────────────────────────────────────
public record SubscriptionTierDto(
    string TierCode,
    string DisplayName,
    decimal PriceMonthly,
    string Currency,
    TierQuotasDto Quotas
);

public record TierQuotasDto(
    int Jd,
    int GapAnalysis,
    int Assessment,
    int RoadmapActive,
    int CareerTrack,
    int PortfolioCertificate,
    int PortfolioProject,
    bool FullGapHistory
);

// ─── My Subscription ─────────────────────────────────────
public record SubscriptionStatusDto(
    TierSummaryDto Tier,
    string Status,
    DateTime? ExpiresAt,
    UsageDto Usage
);

public record TierSummaryDto(string TierCode, string DisplayName);

public record UsageDto(
    QuotaItemDto Jd,
    QuotaItemDto GapAnalysis,
    QuotaItemDto Assessment,
    QuotaItemDto RoadmapActive,
    QuotaItemDto CareerTrack,
    QuotaItemDto PortfolioCertificate,
    QuotaItemDto PortfolioProject
);

public record QuotaItemDto(int Used, int Limit, bool NearLimit);

// ─── Create Order ─────────────────────────────────────────
public record CreateOrderRequest(
    string TierCode,
    short DurationMonths
);

public record CreateOrderResponse(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string TransferContent,
    string BankAccount,
    string BankCode,
    string AccountName,
    string QrImageUrl,
    string Status
);

// ─── My Orders (lịch sử thanh toán của user) ──────────────
public record MyPaymentOrderItemDto(
    Guid OrderId,
    string TierCode,
    decimal Amount,
    string Currency,
    string PaymentProvider,
    string? ProviderOrderId,
    string Status,
    short DurationMonths,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

// ─── SePay Webhook ────────────────────────────────────────
public record SepayWebhookPayload(
    long Id,
    string Gateway,
    string TransactionDate,
    string AccountNumber,
    string? Code,
    string Content,
    decimal TransferType,
    decimal TransferAmount,
    decimal Accumulated,
    string? ReferenceCode,
    string? Description
);
