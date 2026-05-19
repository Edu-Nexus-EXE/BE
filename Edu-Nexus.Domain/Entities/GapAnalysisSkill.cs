using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.GapAnalysisSkills;

namespace Edu_Nexus.Domain.Entities;

public partial class GapAnalysisSkill
{
    public Guid Id { get; set; }

    public Guid GapAnalysisId { get; set; }

    public Guid? SkillId { get; set; }

    public string SkillName { get; set; } = null!;

    public GapStatus GapStatus { get; set; }

    public SkillLevel? CurrentLevel { get; set; }

    public SkillLevel TargetLevel { get; set; }

    public short? UrgencyScore { get; set; }

    public string? Reasoning { get; set; }

    public bool IsMandatoryInJd { get; set; }

    public virtual GapAnalysis GapAnalysis { get; set; } = null!;

    public virtual Skill? Skill { get; set; }
}
