namespace Edu_Nexus.Application.DTOs;

public record AdminSkillMappingDto(Guid SkillId, bool IsPrimary, short? SequenceOrder);

public record AdminResourceDto(
    Guid Id,
    string Title,
    string Type,
    string? Provider,
    string Url,
    string? Description,
    bool IsFree,
    string AccessType,
    string? AffiliateLabel,
    decimal? AffiliateCommissionRate,
    string Language,
    int? DurationMinutes,
    bool NeedsAdminReview,
    bool IsActive,
    IReadOnlyList<AdminSkillMappingDto> SkillMappings,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AdminResourceUpsertRequest(
    string Title,
    string Type,
    string? Provider,
    string Url,
    string? Description,
    bool IsFree,
    string AccessType,
    string? AffiliateLabel,
    decimal? AffiliateCommissionRate,
    string Language,
    int? DurationMinutes,
    bool? NeedsAdminReview,
    bool? IsActive,
    IReadOnlyList<AdminSkillMappingDto>? SkillMappings);

public record AdminResourceReviewRequest(bool NeedsAdminReview);
