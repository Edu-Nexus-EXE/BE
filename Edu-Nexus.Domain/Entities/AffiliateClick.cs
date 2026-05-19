using System;
using System.Collections.Generic;
using System.Net;

namespace Edu_Nexus.Domain.Entities;

public partial class AffiliateClick
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid ResourceId { get; set; }

    public Guid? RoadmapNodeId { get; set; }

    public string RedirectUrl { get; set; } = null!;

    public IPAddress? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? ConvertedAt { get; set; }

    public decimal? CommissionAmount { get; set; }

    public DateTime ClickedAt { get; set; }

    public virtual LearningResource Resource { get; set; } = null!;

    public virtual RoadmapNode? RoadmapNode { get; set; }

    public virtual User? User { get; set; }
}
