using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class Skill
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Major { get; set; } = null!;

    public string? Description { get; set; }

    public short DifficultyLevel { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<GapAnalysisSkill> GapAnalysisSkills { get; set; } = new List<GapAnalysisSkill>();

    public virtual ICollection<JdSkill> JdSkills { get; set; } = new List<JdSkill>();

    public virtual ICollection<RoadmapNode> RoadmapNodes { get; set; } = new List<RoadmapNode>();

    public virtual ICollection<SkillResource> SkillResources { get; set; } = new List<SkillResource>();

    public virtual ICollection<Skill> Prerequisites { get; set; } = new List<Skill>();

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}
