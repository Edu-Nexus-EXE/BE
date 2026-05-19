using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.AssessmentSessions;

namespace Edu_Nexus.Domain.Entities;

public partial class AssessmentSession
{
    public Guid Id { get; set; }

    public Guid AssessmentPathId { get; set; }

    public Guid UserId { get; set; }

    public string JobRoleCategorySnapshot { get; set; } = null!;

    public short Part1Count { get; set; }

    public short Part2Count { get; set; }

    public string? SkillScores { get; set; }

    public AssessmentSessionStatus Status { get; set; }

    public bool IsCurrent { get; set; }

    public Guid? ReusedFromSessionId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AssessmentAnswer> AssessmentAnswers { get; set; } = new List<AssessmentAnswer>();

    public virtual AssessmentPath AssessmentPath { get; set; } = null!;

    public virtual ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = new List<AssessmentQuestion>();

    public virtual ICollection<AssessmentSession> InverseReusedFromSession { get; set; } = new List<AssessmentSession>();

    public virtual AssessmentSession? ReusedFromSession { get; set; }

    public virtual User User { get; set; } = null!;
}
