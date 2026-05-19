using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.SubscriptionRenewalNotifications;

namespace Edu_Nexus.Domain.Entities;

public partial class SubscriptionRenewalNotification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SubscriptionId { get; set; }

    public SubscriptionRenewalNotificationType NotificationType { get; set; }

    public DateTime SentAt { get; set; }

    public virtual UserSubscription Subscription { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
