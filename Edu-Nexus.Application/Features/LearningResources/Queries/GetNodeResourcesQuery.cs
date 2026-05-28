using Edu_Nexus.Application.Features.LearningResources.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.Roadmaps;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Edu_Nexus.Application.Features.LearningResources.Queries;

public record GetNodeResourcesQuery(Guid NodeId) : IRequest<List<NodeResourceDto>>;

public class GetNodeResourcesQueryHandler : IRequestHandler<GetNodeResourcesQuery, List<NodeResourceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetNodeResourcesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<NodeResourceDto>> Handle(GetNodeResourcesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        // 1. Find the roadmap node and its parent roadmap
        var node = await _unitOfWork.RoadmapNodes
            .FirstOrDefaultAsync(n => n.Id == request.NodeId, "Roadmap", cancellationToken);

        if (node == null)
            throw new Exception("404 NOT_FOUND");

        // 2. Verify ownership
        if (node.Roadmap.UserId != userId)
            throw new Exception("404 NOT_FOUND");

        // 3. Check roadmap is not archived
        if (node.Roadmap.Status == RoadmapStatus.Archived)
            throw new Exception("403 ROADMAP_ARCHIVED");

        // 4. If no skill linked to this node, return empty
        if (node.SkillId == null)
            return new List<NodeResourceDto>();

        // 5. Query skill_resources for this skill, include the learning resource
        var skillResources = (await _unitOfWork.SkillResources
            .FindAsync(
                sr => sr.SkillId == node.SkillId.Value,
                "Resource",
                cancellationToken))
            .Where(sr => sr.Resource.IsActive) // Only active resources (Appendix A.1)
            .ToList();

        // 6. Load user onboarding preferences for ranking (FR5.2)
        var onboarding = await _unitOfWork.OnboardingResponses
            .FirstOrDefaultAsync(o => o.UserId == userId, "", cancellationToken);

        // 7. Rank resources based on user preferences
        var ranked = RankResources(skillResources, onboarding);

        // 8. Map to DTO
        return ranked.Select(sr => new NodeResourceDto
        {
            Id = sr.Resource.Id,
            Title = sr.Resource.Title,
            Type = sr.Resource.Type.ToString().ToLowerInvariant(),
            Provider = sr.Resource.Provider,
            Url = sr.Resource.Url,
            IsFree = sr.Resource.IsFree,
            AccessType = sr.Resource.AccessType.ToString().ToLowerInvariant(),
            AffiliateLabel = sr.Resource.AffiliateLabel,
            Language = sr.Resource.Language,
            DurationMinutes = sr.Resource.DurationMinutes,
            IsPrimary = sr.IsPrimary
        }).ToList();
    }

    /// <summary>
    /// Rank resources based on user onboarding preferences (FR5.2):
    /// - Primary resources first
    /// - If user budget is tight ("free" or "under_100k"), prefer free resources
    /// - Match preferred_channel to resource type (e.g. "video" → Video type first)
    /// - Sequence order as tiebreaker
    /// </summary>
    private static List<Domain.Entities.SkillResource> RankResources(
        List<Domain.Entities.SkillResource> resources,
        Domain.Entities.OnboardingResponse? onboarding)
    {
        var preferFree = onboarding?.LearningBudget is "free" or "under_100k";
        var preferredType = onboarding?.PreferredChannel?.ToLowerInvariant();

        return resources
            .OrderByDescending(sr => sr.IsPrimary)                                          // Primary first
            .ThenByDescending(sr => preferFree && sr.Resource.IsFree)                       // Free first if budget-conscious
            .ThenByDescending(sr => MatchesPreferredChannel(sr.Resource.Type.ToString().ToLowerInvariant(), preferredType)) // Match channel preference
            .ThenBy(sr => sr.SequenceOrder ?? short.MaxValue)                               // Sequence order
            .ToList();
    }

    /// <summary>
    /// Check if a resource type matches the user's preferred learning channel.
    /// E.g. preferred "video" matches Video type, "reading" matches Article/Documentation.
    /// </summary>
    private static bool MatchesPreferredChannel(string resourceType, string? preferredChannel)
    {
        if (string.IsNullOrEmpty(preferredChannel)) return false;

        return preferredChannel switch
        {
            "video" => resourceType == "video",
            "reading" or "article" => resourceType is "article" or "documentation",
            "course" or "structured" => resourceType == "course",
            _ => false
        };
    }
}
