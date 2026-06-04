namespace Edu_Nexus.Application.DTOs;

public record UpdateSubscriptionTierRequest(
    decimal? PriceMonthly,
    int? JdQuota,
    int? GapAnalysisQuota,
    int? AssessmentQuota,
    int? RoadmapActiveQuota,
    int? CareerTrackQuota,
    int? PortfolioCertificateQuota,
    int? PortfolioProjectQuota,
    bool? FullGapHistory,
    bool? IsActive);

public record AdminSubscriptionTierDto(
    Guid Id,
    string TierCode,
    string DisplayName,
    decimal PriceMonthly,
    string Currency,
    int JdQuota,
    int GapAnalysisQuota,
    int AssessmentQuota,
    int RoadmapActiveQuota,
    int CareerTrackQuota,
    int PortfolioCertificateQuota,
    int PortfolioProjectQuota,
    bool FullGapHistory,
    bool IsActive);
