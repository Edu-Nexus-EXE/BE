using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class PaymentOrder
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? SubscriptionId { get; set; }

    public Guid TierId { get; set; }

    public short DurationMonths { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string PaymentProvider { get; set; } = null!;

    public string? ProviderOrderId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual UserSubscription? Subscription { get; set; }

    public virtual SubscriptionTier Tier { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
