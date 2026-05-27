using System;

namespace Edu_Nexus.Application.Features.CareerTracks.DTOs;

public class CareerTrackDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int JdCount { get; set; }
    public int OverallProgress { get; set; }
    public DateTime CreatedAt { get; set; }
}
