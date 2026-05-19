using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.Roadmaps;

namespace Edu_Nexus.Domain.Entities;

public partial class Roadmap
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid JdId { get; set; }

    public Guid? GapAnalysisId { get; set; }

    public string Title { get; set; } = null!;

    public int? EstimatedTotalHours { get; set; }

    public RoadmapStatus Status { get; set; }

    public bool IsOutdated { get; set; }

    public short ProgressPercent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual GapAnalysis? GapAnalysis { get; set; }

    public virtual JdSubmission Jd { get; set; } = null!;

    public virtual ICollection<RoadmapNode> RoadmapNodes { get; set; } = new List<RoadmapNode>();

    public virtual User User { get; set; } = null!;
}
