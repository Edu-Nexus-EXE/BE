using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class OnboardingResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string AcademicYear { get; set; } = null!;

    public string Major { get; set; } = null!;

    public string PrimaryGoal { get; set; } = null!;

    public string WeeklyStudyHours { get; set; } = null!;

    public string ProficiencyLevel { get; set; } = null!;

    public string LearningPriority { get; set; } = null!;

    public string LearningBudget { get; set; } = null!;

    public string PreferredChannel { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
