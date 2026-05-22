using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.AssessmentPaths;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.GapAnalyses.Commands;

public record StartGapAnalysisCommand(Guid JdId) : IRequest<GapAnalysisAcceptedDto>;

public class StartGapAnalysisCommandHandler : IRequestHandler<StartGapAnalysisCommand, GapAnalysisAcceptedDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGapAnalysisQueue _queue;

    public StartGapAnalysisCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IGapAnalysisQueue queue)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _queue = queue;
    }

    public async Task<GapAnalysisAcceptedDto> Handle(StartGapAnalysisCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        var jd = await _unitOfWork.JdSubmissions.FirstOrDefaultAsync(
            j => j.Id == request.JdId && j.UserId == userId && j.DeletedAt == null,
            "", cancellationToken)
            ?? throw new Exception("404 JD_NOT_FOUND");

        if (jd.ParseStatus != ParseStatus.Completed)
        {
            throw new Exception("422 JD_NOT_PARSED");
        }

        var path = await _unitOfWork.AssessmentPaths.FirstOrDefaultAsync(
            p => p.JdId == request.JdId && p.UserId == userId, "", cancellationToken)
            ?? throw new Exception("422 ASSESSMENT_PATH_REQUIRED");

        var inputSource = await ResolveInputSourceAsync(path, cancellationToken);

        var existing = (await _unitOfWork.GapAnalyses.FindAsync(
            g => g.JdId == request.JdId && g.UserId == userId,
            "", cancellationToken))
            .OrderByDescending(g => g.Version)
            .ToList();

        var isFirstRun = existing.Count == 0;

        if (isFirstRun)
        {
            await EnforceQuotaAsync(userId, cancellationToken);
        }

        short nextVersion = 1;
        if (existing.Any())
        {
            foreach (var prior in existing.Where(g => g.IsLatest))
            {
                prior.IsLatest = false;
                _unitOfWork.GapAnalyses.Update(prior);
            }
            nextVersion = (short)(existing.Max(g => g.Version) + 1);
        }

        var gap = new GapAnalysis
        {
            UserId = userId,
            JdId = request.JdId,
            AssessmentPathId = path.Id,
            InputSource = inputSource,
            Version = nextVersion,
            IsLatest = true,
            Status = GapAnalysisStatus.Pending,
        };

        _unitOfWork.GapAnalyses.Add(gap);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _queue.Enqueue(gap.Id);

        return new GapAnalysisAcceptedDto(
            gap.Id,
            gap.JdId,
            inputSource.ToString().ToLowerInvariant(),
            gap.Version,
            gap.Status.ToString().ToLowerInvariant());
    }

    private async Task<GapAnalysisInputSource> ResolveInputSourceAsync(AssessmentPath path, CancellationToken ct)
    {
        if (path.PathType == PathType.Cv)
        {
            var cv = await _unitOfWork.CvSubmissions.FirstOrDefaultAsync(
                c => c.AssessmentPathId == path.Id, "", ct);
            if (cv == null || cv.ParseStatus != ParseStatus.Completed)
            {
                throw new Exception("422 CV_NOT_READY");
            }
            return GapAnalysisInputSource.Cv;
        }

        var session = await _unitOfWork.AssessmentSessions.FirstOrDefaultAsync(
            s => s.AssessmentPathId == path.Id && s.IsCurrent && s.Status == AssessmentSessionStatus.Submitted,
            "", ct);
        if (session == null)
        {
            throw new Exception("422 ASSESSMENT_NOT_SUBMITTED");
        }
        return GapAnalysisInputSource.Assessment;
    }

    private async Task EnforceQuotaAsync(Guid userId, CancellationToken ct)
    {
        var subscription = await _unitOfWork.UserSubscriptions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active,
            includeProperties: nameof(UserSubscription.Tier),
            cancellationToken: ct);

        var quota = subscription?.Tier?.GapAnalysisQuota ?? 3;
        if (quota < 0) return;

        var distinctJds = (await _unitOfWork.GapAnalyses.FindAsync(
            g => g.UserId == userId && g.Status == GapAnalysisStatus.Completed,
            "", ct))
            .Select(g => g.JdId)
            .Distinct()
            .Count();

        if (distinctJds >= quota)
        {
            throw new Exception($"403 QUOTA_EXCEEDED|gapAnalysis|{distinctJds}|{quota}");
        }
    }
}
