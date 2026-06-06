using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.AssessmentPaths;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using Edu_Nexus.Domain.Enums.Roadmaps;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.Subscriptions.Queries;

public record GetMySubscriptionQuery : IRequest<SubscriptionStatusDto>;

public class GetMySubscriptionQueryHandler : IRequestHandler<GetMySubscriptionQuery, SubscriptionStatusDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMySubscriptionQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SubscriptionStatusDto> Handle(GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exception("401 UNAUTHORIZED");

        // Lấy subscription hiện tại (nếu không có thì fallback Free tier)
        var subscription = await _unitOfWork.UserSubscriptions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Status == UserSubscriptionStatus.Active,
            "Tier", cancellationToken);

        if (subscription == null)
        {
            var freeTier = await _unitOfWork.SubscriptionTiers.FirstOrDefaultAsync(
                t => t.IsActive && t.PriceMonthly == 0, "", cancellationToken);

            var usage = await CalculateUsageAsync(userId,
                freeTier?.JdQuota ?? 3, freeTier?.GapAnalysisQuota ?? 3,
                freeTier?.AssessmentQuota ?? 3, freeTier?.RoadmapActiveQuota ?? 3,
                freeTier?.CareerTrackQuota ?? 1, freeTier?.PortfolioCertificateQuota ?? 3,
                freeTier?.PortfolioProjectQuota ?? 3, cancellationToken);

            // Spec FR8.2: Free tier is always treated as the active default
            // subscription so the FE can render gating logic with a single
            // status check (== "active"), regardless of whether a Free row
            // is persisted in user_subscriptions.
            return new SubscriptionStatusDto(
                new TierSummaryDto(
                    freeTier?.TierCode.ToString().ToLowerInvariant() ?? "free",
                    freeTier?.DisplayName ?? "Free"),
                "active", null, usage);
        }

        var tier = subscription.Tier;
        var usageDto = await CalculateUsageAsync(userId,
            tier.JdQuota, tier.GapAnalysisQuota, tier.AssessmentQuota,
            tier.RoadmapActiveQuota, tier.CareerTrackQuota,
            tier.PortfolioCertificateQuota, tier.PortfolioProjectQuota,
            cancellationToken);

        return new SubscriptionStatusDto(
            new TierSummaryDto(tier.TierCode.ToString().ToLowerInvariant(), tier.DisplayName),
            subscription.Status.ToString().ToLowerInvariant(),
            subscription.ExpiresAt,
            usageDto);
    }

    private async Task<UsageDto> CalculateUsageAsync(
        Guid userId,
        int jdLimit, int gapLimit, int assessmentLimit,
        int roadmapLimit, int careerTrackLimit,
        int certLimit, int projectLimit,
        CancellationToken ct)
    {
        var jdUsed = (await _unitOfWork.JdSubmissions.FindAsync(
            j => j.UserId == userId && j.DeletedAt == null, "", ct)).Count();

        // FR8.2: gap quota counts DISTINCT JDs that have a *completed* gap.
        // Pending/processing/failed runs do not consume quota.
        var completedGaps = await _unitOfWork.GapAnalyses.FindAsync(
            g => g.UserId == userId && g.Status == GapAnalysisStatus.Completed,
            "", ct);
        var gapUsed = completedGaps.Select(g => g.JdId).Distinct().Count();

        var assessmentPaths = await _unitOfWork.AssessmentPaths.FindAsync(
            p => p.UserId == userId && p.PathType == PathType.Assessment, "Jd", ct);
        var assessmentUsed = assessmentPaths
            .Where(p => p.Jd != null && p.Jd.DeletedAt == null)
            .Select(p => p.JdId).Distinct().Count();

        var roadmapUsed = (await _unitOfWork.Roadmaps.FindAsync(
            r => r.UserId == userId && r.Status == RoadmapStatus.Active, "", ct)).Count();

        var careerTrackUsed = (await _unitOfWork.CareerTracks.FindAsync(
            c => c.UserId == userId, "", ct)).Count();

        var certUsed = (await _unitOfWork.PortfolioCertificates.FindAsync(
            c => c.UserId == userId, "", ct)).Count();
        var projectUsed = (await _unitOfWork.PortfolioProjects.FindAsync(
            p => p.UserId == userId, "", ct)).Count();

        return new UsageDto(
            BuildQuotaItem(jdUsed, jdLimit),
            BuildQuotaItem(gapUsed, gapLimit),
            BuildQuotaItem(assessmentUsed, assessmentLimit),
            BuildQuotaItem(roadmapUsed, roadmapLimit),
            BuildQuotaItem(careerTrackUsed, careerTrackLimit),
            BuildQuotaItem(certUsed, certLimit),
            BuildQuotaItem(projectUsed, projectLimit));
    }

    private static QuotaItemDto BuildQuotaItem(int used, int limit)
    {
        var nearLimit = limit >= 0 && used >= (limit * 2.0 / 3.0);
        return new QuotaItemDto(used, limit, nearLimit);
    }
}
