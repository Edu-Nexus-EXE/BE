using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

public class SubscriptionExpirationJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionExpirationJob> _logger;

    public SubscriptionExpirationJob(IUnitOfWork unitOfWork, ILogger<SubscriptionExpirationJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SubscriptionExpirationJob started checking for expired subscriptions.");

        var now = DateTime.UtcNow;

        // 1. Tìm các subscription Active đã quá hạn
        var expiredSubs = await _unitOfWork.UserSubscriptions.FindAsync(
            s => s.Status == UserSubscriptionStatus.Active && s.ExpiresAt != null && s.ExpiresAt <= now,
            "", cancellationToken);

        var expiredList = expiredSubs.ToList();
        if (expiredList.Count == 0)
        {
            _logger.LogInformation("No expired subscriptions found.");
            return;
        }

        // 2. Tìm Free Tier để gán lại cho user
        var freeTier = await _unitOfWork.SubscriptionTiers.FirstOrDefaultAsync(
            t => t.TierCode == SubscriptionTierCode.Free && t.IsActive,
            "", cancellationToken);

        if (freeTier == null)
        {
            _logger.LogError("Free subscription tier not found or inactive. Cannot downgrade expired subscriptions.");
            return;
        }

        // 3. Downgrade từng user về Free tier
        foreach (var sub in expiredList)
        {
            _logger.LogInformation("Downgrading subscription for user {UserId}. Expired at: {ExpiresAt}",
                sub.UserId, sub.ExpiresAt);

            sub.TierId = freeTier.Id;
            sub.ExpiresAt = null;
            sub.UpdatedAt = now;
            sub.Status = UserSubscriptionStatus.Active;
            
            _unitOfWork.UserSubscriptions.Update(sub);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Downgraded {Count} expired subscriptions to Free tier successfully.", expiredList.Count);
    }
}
