using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using Edu_Nexus.Domain.Enums.Roadmaps;
using MediatR;

namespace Edu_Nexus.Application.Features.Roadmaps.Commands;

public record RegenerateRoadmapCommand(Guid Id) : IRequest<RoadmapAcceptedDto>;

public class RegenerateRoadmapCommandHandler : IRequestHandler<RegenerateRoadmapCommand, RoadmapAcceptedDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRoadmapGenerateQueue _queue;

    public RegenerateRoadmapCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRoadmapGenerateQueue queue)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _queue = queue;
    }

    public async Task<RoadmapAcceptedDto> Handle(RegenerateRoadmapCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var oldRoadmap = await _unitOfWork.Roadmaps.FirstOrDefaultAsync(
            r => r.Id == request.Id && r.UserId == userId,
            "", cancellationToken)
            ?? throw new Exception("404 ROADMAP_NOT_FOUND");

        // 1. Check gap analysis completed
        var gap = await _unitOfWork.GapAnalyses.FirstOrDefaultAsync(
            g => g.JdId == oldRoadmap.JdId && g.UserId == userId && g.IsLatest && g.Status == GapAnalysisStatus.Completed,
            "", cancellationToken)
            ?? throw new Exception("422 GAP_ANALYSIS_NOT_COMPLETED");

        // 2. Archive current
        oldRoadmap.Status = RoadmapStatus.Archived;
        oldRoadmap.IsOutdated = false;
        _unitOfWork.Roadmaps.Update(oldRoadmap);

        // 3. Generate mới
        var newRoadmap = new Roadmap
        {
            UserId = userId,
            JdId = oldRoadmap.JdId,
            Status = RoadmapStatus.Generating,
            Title = null,
            ProgressPercent = 0,
            IsOutdated = false
        };

        _unitOfWork.Roadmaps.Add(newRoadmap);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Enqueue
        _queue.Enqueue(newRoadmap.Id);

        return new RoadmapAcceptedDto(
            newRoadmap.Id,
            newRoadmap.JdId,
            newRoadmap.Status.ToString().ToLowerInvariant(),
            newRoadmap.Title,
            newRoadmap.ProgressPercent);
    }
}
