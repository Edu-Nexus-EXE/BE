using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using Edu_Nexus.Domain.Enums.Roadmaps;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.Roadmaps.Commands;

public record GenerateRoadmapCommand(Guid JdId) : IRequest<RoadmapAcceptedDto>;

public class GenerateRoadmapCommandHandler : IRequestHandler<GenerateRoadmapCommand, RoadmapAcceptedDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRoadmapGenerateQueue _queue;

    public GenerateRoadmapCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IRoadmapGenerateQueue queue)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _queue = queue;
    }

    public async Task<RoadmapAcceptedDto> Handle(GenerateRoadmapCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        // 1. Check gap analysis completed cho JD
        var gap = await _unitOfWork.GapAnalyses.FirstOrDefaultAsync(
            g => g.JdId == request.JdId && g.UserId == userId && g.IsLatest && g.Status == GapAnalysisStatus.Completed,
            "", cancellationToken)
            ?? throw new Exception("422 GAP_ANALYSIS_NOT_COMPLETED");

        // 2. Check quota
        var subscription = await _unitOfWork.UserSubscriptions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active,
            "Tier", cancellationToken);
            
        var quota = subscription?.Tier?.RoadmapActiveQuota ?? 3;
        
        if (quota >= 0)
        {
            var activeRoadmapsCount = (await _unitOfWork.Roadmaps.FindAsync(
                r => r.UserId == userId && r.Status == RoadmapStatus.Active,
                "", cancellationToken))
                .Count();

            var existingForJd = await _unitOfWork.Roadmaps.FindAsync(
                r => r.UserId == userId && r.JdId == request.JdId && r.Status == RoadmapStatus.Active,
                "", cancellationToken);
            
            var hasExistingActiveForThisJd = existingForJd.Any();
            var effectiveCount = hasExistingActiveForThisJd ? activeRoadmapsCount - 1 : activeRoadmapsCount;

            if (effectiveCount >= quota)
            {
                throw new Exception($"403 QUOTA_EXCEEDED|roadmapActive|{activeRoadmapsCount}|{quota}");
            }
        }

        // 3. Nếu đã có active roadmap cùng JD → archive cũ
        var existingRoadmaps = await _unitOfWork.Roadmaps.FindAsync(
            r => r.UserId == userId && r.JdId == request.JdId && r.Status == RoadmapStatus.Active,
            "", cancellationToken);

        foreach (var existing in existingRoadmaps)
        {
            existing.Status = RoadmapStatus.Archived;
            existing.IsOutdated = false;
            _unitOfWork.Roadmaps.Update(existing);
        }

        // 4. INSERT roadmaps với status = 'generating'
        var roadmap = new Roadmap
        {
            UserId = userId,
            JdId = request.JdId,
            Status = RoadmapStatus.Generating,
            Title = null, // Set by AI later
            ProgressPercent = 0,
            IsOutdated = false
        };

        _unitOfWork.Roadmaps.Add(roadmap);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Enqueue
        _queue.Enqueue(roadmap.Id);

        return new RoadmapAcceptedDto(
            roadmap.Id,
            roadmap.JdId,
            roadmap.Status.ToString().ToLowerInvariant(),
            roadmap.Title,
            roadmap.ProgressPercent);
    }
}

public record RoadmapAcceptedDto(Guid Id, Guid JdId, string Status, string? Title, int ProgressPercent);
