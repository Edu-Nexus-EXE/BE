using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class SkillResource
{
    public Guid SkillId { get; set; }

    public Guid ResourceId { get; set; }

    public bool IsPrimary { get; set; }

    public short? SequenceOrder { get; set; }

    public virtual LearningResource Resource { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
