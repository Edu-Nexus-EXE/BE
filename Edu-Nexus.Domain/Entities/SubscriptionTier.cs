using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.SubscriptionTiers;

namespace Edu_Nexus.Domain.Entities;

public partial class SubscriptionTier
{
    public Guid Id { get; set; }

    public SubscriptionTierCode TierCode { get; set; }

    public string DisplayName { get; set; } = null!;

    public decimal PriceMonthly { get; set; }

    public string Currency { get; set; } = null!;

    public int JdQuota { get; set; }

    public int GapAnalysisQuota { get; set; }

    public int AssessmentQuota { get; set; }

    public int RoadmapActiveQuota { get; set; }

    public int CareerTrackQuota { get; set; }

    public int PortfolioCertificateQuota { get; set; }

    public int PortfolioProjectQuota { get; set; }

    public bool FullGapHistory { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();

    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
