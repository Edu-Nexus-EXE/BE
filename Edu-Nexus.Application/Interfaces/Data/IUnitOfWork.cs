using Edu_Nexus.Domain.Entities;

namespace Edu_Nexus.Application.Interfaces.Data;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<SubscriptionTier> SubscriptionTiers { get; }
    IRepository<UserSubscription> UserSubscriptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
