using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class JdSubmission
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string SourceType { get; set; } = null!;

    public string? SourceUrl { get; set; }

    public string? RawContent { get; set; }

    public string? JobTitle { get; set; }

    public string? JobRoleCategory { get; set; }

    public string? SeniorityLevel { get; set; }

    public int? SalaryMin { get; set; }

    public int? SalaryMax { get; set; }

    public string? Currency { get; set; }

    public string ParseStatus { get; set; } = null!;

    public string? ParseError { get; set; }

    public DateTime? ParsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual AssessmentPath? AssessmentPath { get; set; }

    public virtual ICollection<CareerTrackJd> CareerTrackJds { get; set; } = new List<CareerTrackJd>();

    public virtual GapAnalysis? GapAnalysis { get; set; }

    public virtual ICollection<JdSkill> JdSkills { get; set; } = new List<JdSkill>();

    public virtual Roadmap? Roadmap { get; set; }

    public virtual User User { get; set; } = null!;
}
