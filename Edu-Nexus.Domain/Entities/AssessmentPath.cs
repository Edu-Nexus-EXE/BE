using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class AssessmentPath
{
    public Guid Id { get; set; }

    public Guid JdId { get; set; }

    public Guid UserId { get; set; }

    public string PathType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AssessmentSession? AssessmentSession { get; set; }

    public virtual CvSubmission? CvSubmission { get; set; }

    public virtual ICollection<GapAnalysis> GapAnalyses { get; set; } = new List<GapAnalysis>();

    public virtual JdSubmission Jd { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
