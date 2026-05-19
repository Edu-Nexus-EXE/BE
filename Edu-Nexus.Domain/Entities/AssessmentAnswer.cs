using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.AssessmentQuestions;

namespace Edu_Nexus.Domain.Entities;

public partial class AssessmentAnswer
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public Guid QuestionId { get; set; }

    public AssessmentOption SelectedOption { get; set; }

    public bool IsCorrect { get; set; }

    public DateTime AnsweredAt { get; set; }

    public virtual AssessmentQuestion Question { get; set; } = null!;

    public virtual AssessmentSession Session { get; set; } = null!;
}
