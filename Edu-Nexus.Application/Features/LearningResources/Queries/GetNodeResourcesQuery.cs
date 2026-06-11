using Edu_Nexus.Application.Features.LearningResources.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
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

        // 3. Archived roadmap vẫn cho XEM resource (read-only) — user thường xem lại lộ trình cũ
        //    sau khi regenerate (mỗi lần regenerate archive roadmap cũ). Trả 403 ở đây làm FE
        //    hiển thị lỗi nhiễu. Chỉ chặn ghi/tương tác ở chỗ khác, không chặn đọc resource.

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

        // 7. Map to DTO
        var resources = skillResources.Select(sr => new NodeResourceDto
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

        // 8. Rank resources based on onboarding preferences; skip ranking if no onboarding
        if (onboarding != null)
            resources = resources.OrderByDescending(r => RankResource(r, onboarding.LearningBudget, onboarding.PreferredChannel)).ToList();

        return resources;
    }

    private static int RankResource(NodeResourceDto r, string? budget, string? channel)
    {
        int score = 0;
        if (string.Equals(budget, "free", StringComparison.OrdinalIgnoreCase) && r.IsFree) score += 10;
        if (string.Equals(channel, "Video", StringComparison.OrdinalIgnoreCase) && r.Type == "video") score += 10;
        if (string.Equals(channel, "Đọc tài liệu", StringComparison.OrdinalIgnoreCase)
            && (r.Type == "article" || r.Type == "documentation")) score += 10;
        if (r.Language == "vi") score += 3;
        if (r.IsPrimary) score += 5;
        return score;
    }
}
