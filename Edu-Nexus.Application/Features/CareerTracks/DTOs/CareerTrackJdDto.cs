using System;

namespace Edu_Nexus.Application.Features.CareerTracks.DTOs;

public class CareerTrackJdDto
{
    public Guid JdId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string RoadmapStatus { get; set; } = string.Empty;
    public int RoadmapProgress { get; set; }
    public DateTime AddedAt { get; set; }
}
