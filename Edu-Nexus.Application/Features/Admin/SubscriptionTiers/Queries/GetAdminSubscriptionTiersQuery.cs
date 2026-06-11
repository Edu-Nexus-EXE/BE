using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.SubscriptionTiers.Queries;

public record GetAdminSubscriptionTiersQuery : IRequest<List<AdminSubscriptionTierDto>>;

public class GetAdminSubscriptionTiersQueryHandler : IRequestHandler<GetAdminSubscriptionTiersQuery, List<AdminSubscriptionTierDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminSubscriptionTiersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<AdminSubscriptionTierDto>> Handle(GetAdminSubscriptionTiersQuery request, CancellationToken cancellationToken)
    {
        // Admin quản trị nên trả TẤT CẢ tier (kể cả is_active=false) để cấu hình.
        var tiers = await _unitOfWork.SubscriptionTiers.FindAsync(t => true, "", cancellationToken);

        return tiers
            .OrderBy(t => t.PriceMonthly)
            .Select(t => new AdminSubscriptionTierDto(
                t.Id,
                t.TierCode.ToString().ToLowerInvariant(),
                t.DisplayName,
                t.PriceMonthly,
                t.Currency,
                t.JdQuota,
                t.GapAnalysisQuota,
                t.AssessmentQuota,
                t.RoadmapActiveQuota,
                t.CareerTrackQuota,
                t.PortfolioCertificateQuota,
                t.PortfolioProjectQuota,
                t.FullGapHistory,
                t.IsActive))
            .ToList();
    }
}
