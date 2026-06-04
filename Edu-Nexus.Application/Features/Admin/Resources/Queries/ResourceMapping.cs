using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.LearningResources;

namespace Edu_Nexus.Application.Features.Admin.Resources.Queries;

internal static class ResourceMapping
{
    public static LearningResourceType? ParseType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Trim().ToLowerInvariant() switch
        {
            "video" => LearningResourceType.Video,
            "article" => LearningResourceType.Article,
            "course" => LearningResourceType.Course,
            "documentation" => LearningResourceType.Documentation,
            "fptu_internal" => LearningResourceType.FptuInternal,
            _ => throw new Exception("422 INVALID_RESOURCE_TYPE")
        };
    }

    public static LearningResourceAccessType ParseAccessType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new Exception("422 INVALID_ACCESS_TYPE");
        return raw.Trim().ToLowerInvariant() switch
        {
            "free" => LearningResourceAccessType.Free,
            "fptu_internal" => LearningResourceAccessType.FptuInternal,
            "affiliate" => LearningResourceAccessType.Affiliate,
            "partnership_premium" => LearningResourceAccessType.PartnershipPremium,
            "partnership_subscription" => LearningResourceAccessType.PartnershipSubscription,
            _ => throw new Exception("422 INVALID_ACCESS_TYPE")
        };
    }

    public static string TypeToString(LearningResourceType type) => type switch
    {
        LearningResourceType.FptuInternal => "fptu_internal",
        _ => type.ToString().ToLowerInvariant()
    };

    public static string AccessTypeToString(LearningResourceAccessType type) => type switch
    {
        LearningResourceAccessType.FptuInternal => "fptu_internal",
        LearningResourceAccessType.PartnershipPremium => "partnership_premium",
        LearningResourceAccessType.PartnershipSubscription => "partnership_subscription",
        _ => type.ToString().ToLowerInvariant()
    };

    public static AdminResourceDto ToDto(LearningResource r) => new(
        r.Id,
        r.Title,
        TypeToString(r.Type),
        r.Provider,
        r.Url,
        r.Description,
        r.IsFree,
        AccessTypeToString(r.AccessType),
        r.AffiliateLabel,
        r.AffiliateCommissionRate,
        r.Language,
        r.DurationMinutes,
        r.NeedsAdminReview,
        r.IsActive,
        r.SkillResources?
            .OrderBy(sr => sr.SequenceOrder ?? short.MaxValue)
            .Select(sr => new AdminSkillMappingDto(sr.SkillId, sr.IsPrimary, sr.SequenceOrder))
            .ToList() ?? new List<AdminSkillMappingDto>(),
        r.CreatedAt,
        r.UpdatedAt);
}
