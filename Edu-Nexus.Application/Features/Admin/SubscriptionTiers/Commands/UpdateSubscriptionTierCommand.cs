using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Admin;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.SubscriptionTiers.Commands;

public record UpdateSubscriptionTierCommand(string TierCode, UpdateSubscriptionTierRequest Request) : IRequest<AdminSubscriptionTierDto>;

public class UpdateSubscriptionTierCommandHandler : IRequestHandler<UpdateSubscriptionTierCommand, AdminSubscriptionTierDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAdminAuditLogger _audit;

    public UpdateSubscriptionTierCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAdminAuditLogger audit)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _audit = audit;
    }

    public async Task<AdminSubscriptionTierDto> Handle(UpdateSubscriptionTierCommand request, CancellationToken cancellationToken)
    {
        var adminId = _currentUserService.UserId ?? throw new Exception("401 UNAUTHORIZED");

        var code = request.TierCode?.Trim().ToLowerInvariant();
        SubscriptionTierCode parsedCode = code switch
        {
            "free" => SubscriptionTierCode.Free,
            "student" => SubscriptionTierCode.Student,
            _ => throw new Exception("404 TIER_NOT_FOUND")
        };

        var tier = await _unitOfWork.SubscriptionTiers.FirstOrDefaultAsync(
            t => t.TierCode == parsedCode, "", cancellationToken)
            ?? throw new Exception("404 TIER_NOT_FOUND");

        var changes = new Dictionary<string, object?>();

        if (request.Request.PriceMonthly.HasValue)
        {
            if (request.Request.PriceMonthly.Value < 0) throw new Exception("422 INVALID_PRICE");
            changes["priceMonthly"] = new { from = tier.PriceMonthly, to = request.Request.PriceMonthly.Value };
            tier.PriceMonthly = request.Request.PriceMonthly.Value;
        }

        ApplyQuota(tier, request.Request.JdQuota, v => tier.JdQuota = v, v => tier.JdQuota, "jdQuota", changes);
        ApplyQuota(tier, request.Request.GapAnalysisQuota, v => tier.GapAnalysisQuota = v, v => tier.GapAnalysisQuota, "gapAnalysisQuota", changes);
        ApplyQuota(tier, request.Request.AssessmentQuota, v => tier.AssessmentQuota = v, v => tier.AssessmentQuota, "assessmentQuota", changes);
        ApplyQuota(tier, request.Request.RoadmapActiveQuota, v => tier.RoadmapActiveQuota = v, v => tier.RoadmapActiveQuota, "roadmapActiveQuota", changes);
        ApplyQuota(tier, request.Request.CareerTrackQuota, v => tier.CareerTrackQuota = v, v => tier.CareerTrackQuota, "careerTrackQuota", changes);
        ApplyQuota(tier, request.Request.PortfolioCertificateQuota, v => tier.PortfolioCertificateQuota = v, v => tier.PortfolioCertificateQuota, "portfolioCertificateQuota", changes);
        ApplyQuota(tier, request.Request.PortfolioProjectQuota, v => tier.PortfolioProjectQuota = v, v => tier.PortfolioProjectQuota, "portfolioProjectQuota", changes);

        if (request.Request.FullGapHistory.HasValue && tier.FullGapHistory != request.Request.FullGapHistory.Value)
        {
            changes["fullGapHistory"] = new { from = tier.FullGapHistory, to = request.Request.FullGapHistory.Value };
            tier.FullGapHistory = request.Request.FullGapHistory.Value;
        }

        if (request.Request.IsActive.HasValue && tier.IsActive != request.Request.IsActive.Value)
        {
            changes["isActive"] = new { from = tier.IsActive, to = request.Request.IsActive.Value };
            tier.IsActive = request.Request.IsActive.Value;
        }

        if (changes.Count > 0)
        {
            _unitOfWork.SubscriptionTiers.Update(tier);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(
                adminId,
                "update_subscription_tier",
                "subscription_tier",
                tier.Id,
                new { tierCode = code, changes },
                cancellationToken);
        }

        return new AdminSubscriptionTierDto(
            tier.Id,
            tier.TierCode.ToString().ToLowerInvariant(),
            tier.DisplayName,
            tier.PriceMonthly,
            tier.Currency,
            tier.JdQuota,
            tier.GapAnalysisQuota,
            tier.AssessmentQuota,
            tier.RoadmapActiveQuota,
            tier.CareerTrackQuota,
            tier.PortfolioCertificateQuota,
            tier.PortfolioProjectQuota,
            tier.FullGapHistory,
            tier.IsActive);
    }

    private static void ApplyQuota(
        Edu_Nexus.Domain.Entities.SubscriptionTier tier,
        int? newValue,
        Action<int> setter,
        Func<Edu_Nexus.Domain.Entities.SubscriptionTier, int> reader,
        string key,
        Dictionary<string, object?> changes)
    {
        if (!newValue.HasValue) return;
        if (newValue.Value < -1) throw new Exception($"422 INVALID_QUOTA|{key}");

        var current = reader(tier);
        if (current == newValue.Value) return;

        changes[key] = new { from = current, to = newValue.Value };
        setter(newValue.Value);
    }
}
