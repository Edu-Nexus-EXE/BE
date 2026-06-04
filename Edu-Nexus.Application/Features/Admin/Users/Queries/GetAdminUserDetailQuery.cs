using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.GapAnalyses;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using Edu_Nexus.Domain.Enums.Roadmaps;
using Edu_Nexus.Domain.Enums.AssessmentSessions;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Users.Queries;

public record GetAdminUserDetailQuery(Guid UserId) : IRequest<AdminUserDetailDto>;

public class GetAdminUserDetailQueryHandler : IRequestHandler<GetAdminUserDetailQuery, AdminUserDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminUserDetailQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminUserDetailDto> Handle(GetAdminUserDetailQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            u => u.Id == request.UserId && u.DeletedAt == null,
            $"{nameof(User.UserSubscription)}.{nameof(UserSubscription.Tier)}",
            cancellationToken)
            ?? throw new Exception("404 USER_NOT_FOUND");

        var subscriptionDto = user.UserSubscription == null
            ? null
            : new AdminUserSubscriptionDto(
                user.UserSubscription.Tier.TierCode.ToString().ToLowerInvariant(),
                user.UserSubscription.Tier.DisplayName,
                user.UserSubscription.Status.ToString().ToLowerInvariant(),
                user.UserSubscription.StartedAt,
                user.UserSubscription.ExpiresAt,
                user.UserSubscription.AutoRenew);

        var tier = user.UserSubscription?.Tier;
        var usage = await BuildUsageAsync(user.Id, tier, cancellationToken);

        var orders = (await _unitOfWork.PaymentOrders.FindAsync(
            o => o.UserId == user.Id, "", cancellationToken))
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .Select(o => new AdminPaymentHistoryDto(
                o.Id,
                o.Amount,
                o.Currency,
                o.PaymentProvider.ToString().ToLowerInvariant() == "manualtransfer"
                    ? "manual_transfer"
                    : o.PaymentProvider.ToString().ToLowerInvariant(),
                o.ProviderOrderId,
                o.Status.ToString().ToLowerInvariant(),
                o.DurationMonths,
                o.CreatedAt,
                o.CompletedAt))
            .ToList();

        return new AdminUserDetailDto(
            user.Id,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.IsBanned,
            user.IsSurveyCompleted,
            user.CreatedAt,
            user.LastLoginAt,
            subscriptionDto,
            usage,
            orders);
    }

    private async Task<Dictionary<string, AdminUserUsageEntry>> BuildUsageAsync(Guid userId, SubscriptionTier? tier, CancellationToken ct)
    {
        var distinctJdsWithGap = (await _unitOfWork.GapAnalyses.FindAsync(
            g => g.UserId == userId && g.Status == GapAnalysisStatus.Completed, "", ct))
            .Select(g => g.JdId).Distinct().Count();

        var jdCount = (await _unitOfWork.JdSubmissions.FindAsync(
            j => j.UserId == userId && j.DeletedAt == null, "", ct)).Count();

        var distinctJdsWithAssessment = (await _unitOfWork.AssessmentSessions.FindAsync(
            s => s.UserId == userId && s.Status == AssessmentSessionStatus.Submitted, $"{nameof(AssessmentSession.AssessmentPath)}", ct))
            .Where(s => s.AssessmentPath != null)
            .Select(s => s.AssessmentPath!.JdId).Distinct().Count();

        var activeRoadmaps = (await _unitOfWork.Roadmaps.FindAsync(
            r => r.UserId == userId && r.Status == RoadmapStatus.Active, "", ct)).Count();

        return new Dictionary<string, AdminUserUsageEntry>
        {
            ["jd"] = new(jdCount, tier?.JdQuota ?? -1),
            ["gapAnalysis"] = new(distinctJdsWithGap, tier?.GapAnalysisQuota ?? -1),
            ["assessment"] = new(distinctJdsWithAssessment, tier?.AssessmentQuota ?? -1),
            ["roadmapActive"] = new(activeRoadmaps, tier?.RoadmapActiveQuota ?? -1),
        };
    }
}
