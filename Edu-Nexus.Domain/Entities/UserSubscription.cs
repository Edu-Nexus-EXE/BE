using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.UserSubscriptions;

namespace Edu_Nexus.Domain.Entities;

public partial class UserSubscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TierId { get; set; }

    public UserSubscriptionStatus Status { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool AutoRenew { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();

    public virtual ICollection<SubscriptionRenewalNotification> SubscriptionRenewalNotifications { get; set; } = new List<SubscriptionRenewalNotification>();

    public virtual SubscriptionTier Tier { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
