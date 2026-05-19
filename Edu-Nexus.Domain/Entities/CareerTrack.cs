using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class CareerTrack
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CareerTrackJd> CareerTrackJds { get; set; } = new List<CareerTrackJd>();

    public virtual User User { get; set; } = null!;
}
