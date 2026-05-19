using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class Portfolio
{
    public Guid UserId { get; set; }

    public string? Headline { get; set; }

    public string? Bio { get; set; }

    public string? CoverImageUrl { get; set; }

    public bool ShowCompletedSkills { get; set; }

    public bool ShowCertificates { get; set; }

    public bool ShowProjects { get; set; }

    public bool IsPublic { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
