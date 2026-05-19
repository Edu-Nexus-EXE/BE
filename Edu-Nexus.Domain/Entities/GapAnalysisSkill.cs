using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class GapAnalysisSkill
{
    public Guid Id { get; set; }

    public Guid GapAnalysisId { get; set; }

    public Guid? SkillId { get; set; }

    public string SkillName { get; set; } = null!;

    public string GapStatus { get; set; } = null!;

    public string? CurrentLevel { get; set; }

    public string TargetLevel { get; set; } = null!;

    public short? UrgencyScore { get; set; }

    public string? Reasoning { get; set; }

    public bool IsMandatoryInJd { get; set; }

    public virtual GapAnalysis GapAnalysis { get; set; } = null!;

    public virtual Skill? Skill { get; set; }
}
