using System;
using System.Collections.Generic;

namespace Edu_Nexus.Application.DTOs;

public class PortfolioResponseData
{
    public Guid UserId { get; set; }
    public string? Slug { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool ShowCompletedSkills { get; set; }
    public bool ShowCertificates { get; set; }
    public bool ShowProjects { get; set; }
    public bool IsPublic { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<CompletedSkillDto>? CompletedSkills { get; set; }
    public List<PortfolioCertificateDto>? Certificates { get; set; }
    public List<PortfolioProjectDto>? Projects { get; set; }
}

public class CompletedSkillDto
{
    public Guid SkillId { get; set; }
    public string Name { get; set; } = null!;
}

public class PortfolioCertificateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Issuer { get; set; }
    public string? IssuedDate { get; set; }
    public string? ExpiresDate { get; set; }
    public string? CredentialUrl { get; set; }
    public string? FileUrl { get; set; }
    public bool IsVisible { get; set; }
}

public class PortfolioProjectDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? RepoUrl { get; set; }
    public string? LiveUrl { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? TechStack { get; set; }
    public string? Role { get; set; }
    public string? StartedDate { get; set; }
    public string? CompletedDate { get; set; }
    public bool IsVisible { get; set; }
}

public class UpdatePortfolioRequest
{
    public string? Headline { get; set; }
    public string? Bio { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool ShowCompletedSkills { get; set; }
    public bool ShowCertificates { get; set; }
    public bool ShowProjects { get; set; }
    public bool IsPublic { get; set; }
}

public class AddCertificateRequest
{
    public string Name { get; set; } = null!;
    public string? Issuer { get; set; }
    public string? IssuedDate { get; set; }
    public string? ExpiresDate { get; set; }
    public string? CredentialUrl { get; set; }
    public string? FileUrl { get; set; }
    public bool IsVisible { get; set; }
}

public class AddProjectRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? RepoUrl { get; set; }
    public string? LiveUrl { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? TechStack { get; set; }
    public string? Role { get; set; }
    public string? StartedDate { get; set; }
    public string? CompletedDate { get; set; }
    public bool IsVisible { get; set; }
}

public class ToggleVisibilityRequest
{
    public bool IsVisible { get; set; }
}
