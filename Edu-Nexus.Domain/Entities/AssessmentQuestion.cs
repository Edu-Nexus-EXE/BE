using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.AssessmentQuestions;

namespace Edu_Nexus.Domain.Entities;

public partial class AssessmentQuestion
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public short SequenceOrder { get; set; }

    public AssessmentQuestionPart Part { get; set; }

    public string QuestionText { get; set; } = null!;

    public string Options { get; set; } = null!;

    public AssessmentOption CorrectOption { get; set; }

    public string? RelatedSkill { get; set; }

    public string? Explanation { get; set; }

    public virtual ICollection<AssessmentAnswer> AssessmentAnswers { get; set; } = new List<AssessmentAnswer>();

    public virtual AssessmentSession Session { get; set; } = null!;
}
