using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class CareerTrackJd
{
    public Guid CareerTrackId { get; set; }

    public Guid JdId { get; set; }

    public DateTime AddedAt { get; set; }

    public virtual CareerTrack CareerTrack { get; set; } = null!;

    public virtual JdSubmission Jd { get; set; } = null!;
}
