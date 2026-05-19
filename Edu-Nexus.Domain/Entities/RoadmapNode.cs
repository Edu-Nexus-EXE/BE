using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.RoadmapNodes;

namespace Edu_Nexus.Domain.Entities;

public partial class RoadmapNode
{
    public Guid Id { get; set; }

    public Guid RoadmapId { get; set; }

    public Guid? SkillId { get; set; }

    public string SkillName { get; set; } = null!;

    public string? Description { get; set; }

    public short SequenceOrder { get; set; }

    public int? EstimatedHours { get; set; }

    public bool IsPrerequisite { get; set; }

    public RoadmapNodeStatus Status { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AffiliateClick> AffiliateClicks { get; set; } = new List<AffiliateClick>();

    public virtual Roadmap Roadmap { get; set; } = null!;

    public virtual Skill? Skill { get; set; }

    public virtual ICollection<RoadmapNode> Nodes { get; set; } = new List<RoadmapNode>();

    public virtual ICollection<RoadmapNode> PrerequisiteNodes { get; set; } = new List<RoadmapNode>();
}
