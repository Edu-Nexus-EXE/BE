namespace Edu_Nexus.Application.DTOs;

public record UpdateCurrentUserRequest(
    string FullName,
    string? AvatarUrl,
    string? PortfolioUrlSlug
);
