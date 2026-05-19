using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class GapAnalysis
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid JdId { get; set; }

    public Guid AssessmentPathId { get; set; }

    public string InputSource { get; set; } = null!;

    public short Version { get; set; }

    public bool IsLatest { get; set; }

    public string? Summary { get; set; }

    public string Status { get; set; } = null!;

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual AssessmentPath AssessmentPath { get; set; } = null!;

    public virtual ICollection<GapAnalysisSkill> GapAnalysisSkills { get; set; } = new List<GapAnalysisSkill>();

    public virtual JdSubmission Jd { get; set; } = null!;

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

    public virtual User User { get; set; } = null!;
}
