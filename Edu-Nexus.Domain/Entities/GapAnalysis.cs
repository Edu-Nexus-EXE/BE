using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.GapAnalyses;

namespace Edu_Nexus.Domain.Entities;

public partial class GapAnalysis
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid JdId { get; set; }

    public Guid AssessmentPathId { get; set; }

    public GapAnalysisInputSource InputSource { get; set; }

    public short Version { get; set; }

    public bool IsLatest { get; set; }

    public string? Summary { get; set; }

    public GapAnalysisStatus Status { get; set; }

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual AssessmentPath AssessmentPath { get; set; } = null!;

    public virtual ICollection<GapAnalysisSkill> GapAnalysisSkills { get; set; } = new List<GapAnalysisSkill>();

    public virtual JdSubmission Jd { get; set; } = null!;

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

    public virtual User User { get; set; } = null!;
}
