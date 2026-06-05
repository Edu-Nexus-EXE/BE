using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.SubscriptionRenewalNotifications;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

public class RenewalNotificationJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RenewalNotificationJob> _logger;

    public RenewalNotificationJob(IUnitOfWork unitOfWork, ILogger<RenewalNotificationJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RenewalNotificationJob started checking for subscription renewals.");

        var now = DateTime.UtcNow;
        var today = now.Date;

        // Lấy tất cả active subscriptions có ngày hết hạn
        var activeSubs = await _unitOfWork.UserSubscriptions.FindAsync(
            s => s.Status == UserSubscriptionStatus.Active && s.ExpiresAt != null,
            "User", cancellationToken);

        foreach (var sub in activeSubs)
        {
            var expiresAt = sub.ExpiresAt!.Value;
            var daysUntilExpiry = (expiresAt.Date - today).Days;

            if (daysUntilExpiry == 7)
            {
                await SendNotificationAsync(sub, SubscriptionRenewalNotificationType.Renewal7d, cancellationToken);
            }
            else if (daysUntilExpiry == 3)
            {
                await SendNotificationAsync(sub, SubscriptionRenewalNotificationType.Renewal3d, cancellationToken);
            }
            else if (daysUntilExpiry == 0)
            {
                await SendNotificationAsync(sub, SubscriptionRenewalNotificationType.Renewal0d, cancellationToken);
            }
        }
    }

    private async Task SendNotificationAsync(
        UserSubscription sub,
        SubscriptionRenewalNotificationType type,
        CancellationToken ct)
    {
        // 1. Kiểm tra gửi trùng trong DB
        var alreadySent = await _unitOfWork.SubscriptionRenewalNotifications.FirstOrDefaultAsync(
            n => n.SubscriptionId == sub.Id && n.NotificationType == type,
            "", ct);

        if (alreadySent != null)
        {
            return; // Đã gửi rồi, skip
        }

        // 2. Mô phỏng gửi email bằng Logger (do không có service email thực tế)
        _logger.LogInformation("Sending email notification {Type} to {Email} ({FullName}). Subscription expires at: {ExpiresAt}",
            type, sub.User.Email, sub.User.FullName, sub.ExpiresAt);

        // 3. Lưu log đã gửi vào DB
        var log = new SubscriptionRenewalNotification
        {
            UserId = sub.UserId,
            SubscriptionId = sub.Id,
            NotificationType = type,
            SentAt = DateTime.UtcNow
        };

        _unitOfWork.SubscriptionRenewalNotifications.Add(log);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
