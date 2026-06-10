using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.UserSubscriptions;

namespace Edu_Nexus.Application.Features.Auth.Commands;

public static class FreeSubscriptionFactory
{
    public static UserSubscription? Create(Guid userId, SubscriptionTier? freeTier, DateTime nowUtc)
    {
        if (freeTier == null) return null;

        return new UserSubscription
        {
            UserId = userId,
            TierId = freeTier.Id,
            Status = UserSubscriptionStatus.Active,
            StartedAt = nowUtc,
            ExpiresAt = null,
            AutoRenew = false
        };
    }
}
