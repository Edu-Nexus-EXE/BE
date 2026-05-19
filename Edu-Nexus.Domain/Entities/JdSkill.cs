using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.JdSkills;

namespace Edu_Nexus.Domain.Entities;

public partial class JdSkill
{
    public Guid Id { get; set; }

    public Guid JdId { get; set; }

    public Guid? SkillId { get; set; }

    public string SkillNameRaw { get; set; } = null!;

    public SkillType SkillType { get; set; }

    public bool IsMandatory { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual JdSubmission Jd { get; set; } = null!;

    public virtual Skill? Skill { get; set; }
}
