namespace Edu_Nexus.Application.Features.LearningResources.DTOs;

public class NodeResourceDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public string AccessType { get; set; } = string.Empty;
    public string? AffiliateLabel { get; set; }
    public string Language { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public bool IsPrimary { get; set; }
}
