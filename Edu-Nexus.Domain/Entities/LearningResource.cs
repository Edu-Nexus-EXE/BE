using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class LearningResource
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Provider { get; set; }

    public string Url { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsFree { get; set; }

    public string AccessType { get; set; } = null!;

    public string? AffiliateLabel { get; set; }

    public decimal? AffiliateCommissionRate { get; set; }

    public Guid? PartnerId { get; set; }

    public string Language { get; set; } = null!;

    public int? DurationMinutes { get; set; }

    public bool NeedsAdminReview { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AffiliateClick> AffiliateClicks { get; set; } = new List<AffiliateClick>();

    public virtual ICollection<SkillResource> SkillResources { get; set; } = new List<SkillResource>();
}
