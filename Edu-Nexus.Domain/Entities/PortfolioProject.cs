using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class PortfolioProject
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? RepoUrl { get; set; }

    public string? LiveUrl { get; set; }

    public string? ImageUrl { get; set; }

    public string? TechStack { get; set; }

    public string? Role { get; set; }

    public DateOnly? StartedDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public bool IsVisible { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
