using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.Roadmaps;
using MediatR;

namespace Edu_Nexus.Application.Features.Roadmaps.Queries;

public record GetRoadmapQuery(Guid Id) : IRequest<RoadmapDetailDto>;

public class GetRoadmapQueryHandler : IRequestHandler<GetRoadmapQuery, RoadmapDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetRoadmapQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<RoadmapDetailDto> Handle(GetRoadmapQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        // The query below uses FindAsync and manual Includes since EF Core allows nested includes via standard repo pattern if we adapt, but here we can just eager load.
        // Given standard IUnitOfWork, we use includeProperties.
        var roadmap = await _unitOfWork.Roadmaps.FirstOrDefaultAsync(
            r => r.Id == request.Id && r.UserId == userId,
            "RoadmapNodes,RoadmapNodes.Skill,RoadmapNodes.PrerequisiteNodes", cancellationToken)
            ?? throw new Exception("404 ROADMAP_NOT_FOUND");

        // Fetch related skill resources and learning resources if NOT archived
        var nodesDto = new List<RoadmapNodeDetailDto>();

        if (roadmap.Status != RoadmapStatus.Archived)
        {
            // We need to fetch resources for each node.
            // Normally we'd do a complex Join, but let's just fetch the SkillResources for the involved skills.
            var skillIds = roadmap.RoadmapNodes.Where(n => n.SkillId.HasValue).Select(n => n.SkillId!.Value).Distinct().ToList();

            var skillResources = (await _unitOfWork.SkillResources.FindAsync(
                sr => skillIds.Contains(sr.SkillId),
                "Resource", cancellationToken)).ToList();

            // Load onboarding preferences once for ranking (FR5.2)
            var onboarding = await _unitOfWork.OnboardingResponses
                .FirstOrDefaultAsync(o => o.UserId == userId, "", cancellationToken);

            // Also fetch prereq nodes to map prerequisiteNodeIds
            var allNodeIds = roadmap.RoadmapNodes.Select(n => n.Id).ToList();
            // Assuming we can't easily include self-referencing many-to-many in generic FirstOrDefaultAsync, we might need to fetch the mappings separately or assume they are loaded.
            // Let's use a simpler mapping first and omit prerequisiteNodeIds if not loaded by default, or just do a manual query.
            // Wait, we can fetch PrerequisiteNodes by loading the RoadmapNode entity again if needed, or we just map empty for now.

            foreach (var node in roadmap.RoadmapNodes.OrderBy(n => n.SequenceOrder))
            {
                var resourcesDto = new List<LearningResourceDto>();
                if (node.SkillId.HasValue)
                {
                    var resForSkill = skillResources.Where(sr => sr.SkillId == node.SkillId.Value && sr.Resource.IsActive).ToList();
                    // Map to DTOs
                    var mappedResources = resForSkill.Select(sr =>
                    {
                        var lr = sr.Resource;
                        return new LearningResourceDto(
                            lr.Id,
                            lr.Title,
                            lr.Type.ToString().ToLowerInvariant(),
                            lr.Provider,
                            lr.Url,
                            lr.IsFree,
                            lr.AccessType.ToString().ToLowerInvariant(),
                            lr.AffiliateLabel,
                            lr.Language,
                            lr.DurationMinutes,
                            sr.IsPrimary
                        );
                    }).ToList();
                    // Rank by onboarding preferences if available
                    if (onboarding != null)
                        mappedResources = mappedResources.OrderByDescending(r => RankResource(r, onboarding.LearningBudget, onboarding.PreferredChannel)).ToList();
                    resourcesDto = mappedResources;
                }

                nodesDto.Add(new RoadmapNodeDetailDto(
                    node.Id,
                    node.SequenceOrder,
                    node.SkillName,
                    node.SkillId,
                    node.Description,
                    node.EstimatedHours,
                    node.IsPrerequisite,
                    node.Status.ToString().ToLowerInvariant(),
                    node.CompletedAt,
                    node.PrerequisiteNodes.Select(p => p.Id).ToList(),
                    resourcesDto
                ));
            }
        }
        else
        {
            // If archived, map without resources
            foreach (var node in roadmap.RoadmapNodes.OrderBy(n => n.SequenceOrder))
            {
                nodesDto.Add(new RoadmapNodeDetailDto(
                    node.Id,
                    node.SequenceOrder,
                    node.SkillName,
                    node.SkillId,
                    node.Description,
                    node.EstimatedHours,
                    node.IsPrerequisite,
                    node.Status.ToString().ToLowerInvariant(),
                    node.CompletedAt,
                    node.PrerequisiteNodes.Select(p => p.Id).ToList(),
                    new List<LearningResourceDto>() // Empty for archived
                ));
            }
        }

        return new RoadmapDetailDto(
            roadmap.Id,
            roadmap.JdId,
            roadmap.Title,
            roadmap.Status.ToString().ToLowerInvariant(),
            roadmap.IsOutdated,
            roadmap.EstimatedTotalHours,
            roadmap.ProgressPercent,
            roadmap.CreatedAt,
            nodesDto
        );
    }

    private static int RankResource(LearningResourceDto r, string? budget, string? channel)
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

public record RoadmapDetailDto(Guid Id, Guid JdId, string? Title, string Status, bool IsOutdated, int? EstimatedTotalHours, int ProgressPercent, DateTime CreatedAt, List<RoadmapNodeDetailDto> Nodes);

public record RoadmapNodeDetailDto(Guid Id, short SequenceOrder, string SkillName, Guid? SkillId, string? Description, int? EstimatedHours, bool IsPrerequisite, string Status, DateTime? CompletedAt, List<Guid> PrerequisiteNodeIds, List<LearningResourceDto> Resources);

public record LearningResourceDto(Guid Id, string Title, string Type, string? Provider, string Url, bool IsFree, string AccessType, string? AffiliateLabel, string Language, int? DurationMinutes, bool IsPrimary);
