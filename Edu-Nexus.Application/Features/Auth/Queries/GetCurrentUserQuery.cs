using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using MediatR;

using Edu_Nexus.Application.Interfaces.Security;

namespace Edu_Nexus.Application.Features.Auth.Queries;

public record GetCurrentUserQuery() : IRequest<UserProfileResponseData>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserProfileResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileResponseData> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null)
        {
            throw new Exception("401 UNAUTHORIZED");
        }

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == currentUserId.Value && u.DeletedAt == null, "UserSubscription,UserSubscription.Tier", cancellationToken);

        if (user == null)
        {
            throw new Exception("404 USER_NOT_FOUND");
        }

        SubscriptionDto? subscriptionDto = null;
        if (user.UserSubscription != null)
        {
            subscriptionDto = new SubscriptionDto(
                user.UserSubscription.Tier.TierCode.ToString().ToLower(),
                user.UserSubscription.Tier.DisplayName,
                user.UserSubscription.Status.ToString().ToLower(),
                user.UserSubscription.ExpiresAt
            );
        }

        return new UserProfileResponseData(
            user.Id,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.Role.ToString().ToLower(),
            user.IsSurveyCompleted,
            user.PortfolioUrlSlug,
            subscriptionDto
        );
    }
}
