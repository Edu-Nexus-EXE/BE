using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using MediatR;

namespace Edu_Nexus.Application.Features.Subscriptions.Queries;

public record GetSubscriptionTiersQuery : IRequest<List<SubscriptionTierDto>>;

public class GetSubscriptionTiersQueryHandler : IRequestHandler<GetSubscriptionTiersQuery, List<SubscriptionTierDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSubscriptionTiersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SubscriptionTierDto>> Handle(GetSubscriptionTiersQuery request, CancellationToken cancellationToken)
    {
        var tiers = await _unitOfWork.SubscriptionTiers.FindAsync(
            t => t.IsActive, "", cancellationToken);

        return tiers.Select(t => new SubscriptionTierDto(
            t.TierCode.ToString().ToLowerInvariant(),
            t.DisplayName,
            t.PriceMonthly,
            t.Currency,
            new TierQuotasDto(
                t.JdQuota,
                t.GapAnalysisQuota,
                t.AssessmentQuota,
                t.RoadmapActiveQuota,
                t.CareerTrackQuota,
                t.PortfolioCertificateQuota,
                t.PortfolioProjectQuota,
                t.FullGapHistory
            )
        )).ToList();
    }
}
