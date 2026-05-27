using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.RoadmapNodes;
using Edu_Nexus.Domain.Enums.Roadmaps;
using MediatR;

namespace Edu_Nexus.Application.Features.Roadmaps.Commands;

public record UpdateRoadmapNodeStatusCommand(Guid NodeId, string Status) : IRequest<UpdateRoadmapNodeStatusResponseDto>;

public class UpdateRoadmapNodeStatusCommandHandler : IRequestHandler<UpdateRoadmapNodeStatusCommand, UpdateRoadmapNodeStatusResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateRoadmapNodeStatusCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateRoadmapNodeStatusResponseDto> Handle(UpdateRoadmapNodeStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        if (!Enum.TryParse<RoadmapNodeStatus>(request.Status, true, out var newStatus))
        {
            throw new Exception("400 INVALID_STATUS");
        }

        var node = await _unitOfWork.RoadmapNodes.FirstOrDefaultAsync(
            n => n.Id == request.NodeId && n.Roadmap.UserId == userId,
            "Roadmap,PrerequisiteNodes", cancellationToken)
            ?? throw new Exception("404 NODE_NOT_FOUND");

        if (node.Roadmap.Status != RoadmapStatus.Active)
        {
            throw new Exception("403 ROADMAP_ARCHIVED");
        }

        if (newStatus == RoadmapNodeStatus.Completed)
        {
            // Check prerequisites
            var incompletePrereqs = node.PrerequisiteNodes.Any(p => p.Status != RoadmapNodeStatus.Completed);
            if (incompletePrereqs)
            {
                throw new Exception("422 PREREQUISITES_NOT_MET");
            }
            
            node.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            node.CompletedAt = null;
        }

        node.Status = newStatus;
        _unitOfWork.RoadmapNodes.Update(node);

        // Recalculate roadmap progress
        var allNodes = await _unitOfWork.RoadmapNodes.FindAsync(
            n => n.RoadmapId == node.RoadmapId,
            "", cancellationToken);
            
        var totalNodes = allNodes.Count();
        var completedNodes = allNodes.Count(n => n.Status == RoadmapNodeStatus.Completed);
        // Note: allNodes does not have the updated state of `node` since it's tracked in memory, but EF Core might return the updated entity for the same ID.
        // Let's explicitly calculate it using the modified state.
        completedNodes = allNodes.Count(n => n.Id == node.Id ? newStatus == RoadmapNodeStatus.Completed : n.Status == RoadmapNodeStatus.Completed);

        var progress = totalNodes == 0 ? 0 : (int)Math.Round((double)completedNodes / totalNodes * 100);
        
        node.Roadmap.ProgressPercent = (short)progress;
        _unitOfWork.Roadmaps.Update(node.Roadmap);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateRoadmapNodeStatusResponseDto(
            node.Id,
            node.Status.ToString().ToLowerInvariant(),
            node.CompletedAt,
            progress
        );
    }
}

public record UpdateRoadmapNodeStatusResponseDto(Guid NodeId, string Status, DateTime? CompletedAt, int RoadmapProgressPercent);
