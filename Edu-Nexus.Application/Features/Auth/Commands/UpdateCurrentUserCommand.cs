using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Security;
using MediatR;
using System.Text.RegularExpressions;

namespace Edu_Nexus.Application.Features.Auth.Commands;

public record UpdateCurrentUserCommand(UpdateCurrentUserRequest Request) : IRequest<UserProfileResponseData>;

public class UpdateCurrentUserCommandHandler : IRequestHandler<UpdateCurrentUserCommand, UserProfileResponseData>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCurrentUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileResponseData> Handle(UpdateCurrentUserCommand request, CancellationToken cancellationToken)
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

        if (!string.IsNullOrWhiteSpace(request.Request.FullName))
        {
            user.FullName = request.Request.FullName;
        }

        if (request.Request.AvatarUrl != null)
        {
            user.AvatarUrl = request.Request.AvatarUrl == "" ? null : request.Request.AvatarUrl;
        }

        if (request.Request.PortfolioUrlSlug != null)
        {
            var slug = request.Request.PortfolioUrlSlug == "" ? null : request.Request.PortfolioUrlSlug;
            if (slug != null)
            {
                var slugRegex = new Regex(@"^[a-z0-9-]{3,50}$");
                if (!slugRegex.IsMatch(slug))
                {
                    throw new Exception("422 INVALID_SLUG");
                }

                var existingSlugUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.PortfolioUrlSlug == slug && u.Id != currentUserId.Value && u.DeletedAt == null, "", cancellationToken);
                if (existingSlugUser != null)
                {
                    throw new Exception("409 SLUG_TAKEN");
                }
            }
            user.PortfolioUrlSlug = slug;
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
