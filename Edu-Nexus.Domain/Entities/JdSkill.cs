using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class JdSkill
{
    public Guid Id { get; set; }

    public Guid JdId { get; set; }

    public Guid? SkillId { get; set; }

    public string SkillNameRaw { get; set; } = null!;

    public string SkillType { get; set; } = null!;

    public bool IsMandatory { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual JdSubmission Jd { get; set; } = null!;

    public virtual Skill? Skill { get; set; }
}
