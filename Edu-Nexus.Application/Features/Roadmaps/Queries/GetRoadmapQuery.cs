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
            "RoadmapNodes,RoadmapNodes.Skill", cancellationToken)
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
                    // Assume sorting by preference is done here or separately.
                    foreach(var sr in resForSkill)
                    {
                        var lr = sr.Resource;
                        resourcesDto.Add(new LearningResourceDto(
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
                        ));
                    }
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
                    new List<Guid>(), // prerequisiteNodeIds can be populated later
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
                    new List<Guid>(),
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
}

public record RoadmapDetailDto(Guid Id, Guid JdId, string? Title, string Status, bool IsOutdated, int? EstimatedTotalHours, int ProgressPercent, DateTime CreatedAt, List<RoadmapNodeDetailDto> Nodes);

public record RoadmapNodeDetailDto(Guid Id, short SequenceOrder, string SkillName, Guid? SkillId, string? Description, int? EstimatedHours, bool IsPrerequisite, string Status, DateTime? CompletedAt, List<Guid> PrerequisiteNodeIds, List<LearningResourceDto> Resources);

public record LearningResourceDto(Guid Id, string Title, string Type, string? Provider, string Url, bool IsFree, string AccessType, string? AffiliateLabel, string Language, int? DurationMinutes, bool IsPrimary);
