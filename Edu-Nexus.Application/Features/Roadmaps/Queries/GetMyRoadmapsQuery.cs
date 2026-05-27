using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.Roadmaps;
using MediatR;

namespace Edu_Nexus.Application.Features.Roadmaps.Queries;

public record GetMyRoadmapsQuery(string? Status) : IRequest<IReadOnlyList<MyRoadmapSummaryDto>>;

public class GetMyRoadmapsQueryHandler : IRequestHandler<GetMyRoadmapsQuery, IReadOnlyList<MyRoadmapSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyRoadmapsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<MyRoadmapSummaryDto>> Handle(GetMyRoadmapsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        RoadmapStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<RoadmapStatus>(request.Status, true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }
        }

        var roadmapsQuery = await _unitOfWork.Roadmaps.FindAsync(
            r => r.UserId == userId && (!statusFilter.HasValue || r.Status == statusFilter.Value),
            "Jd,RoadmapNodes", cancellationToken);

        var dtos = roadmapsQuery.Select(r => new MyRoadmapSummaryDto(
            r.Id,
            r.JdId,
            r.Jd?.JobTitle, // Assuming JobTitle is on JdSubmission
            r.Title,
            r.Status.ToString().ToLowerInvariant(),
            r.IsOutdated,
            r.ProgressPercent,
            r.RoadmapNodes.Count,
            r.RoadmapNodes.Count(n => n.Status == Edu_Nexus.Domain.Enums.RoadmapNodes.RoadmapNodeStatus.Completed),
            r.EstimatedTotalHours,
            r.CreatedAt
        )).OrderByDescending(r => r.CreatedAt).ToList();

        return dtos;
    }
}

public record MyRoadmapSummaryDto(Guid Id, Guid JdId, string? JdTitle, string? Title, string Status, bool IsOutdated, int ProgressPercent, int TotalNodes, int CompletedNodes, int? EstimatedTotalHours, DateTime CreatedAt);
