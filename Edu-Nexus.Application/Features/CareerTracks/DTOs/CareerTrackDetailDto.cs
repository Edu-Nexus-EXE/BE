using System;
using System.Collections.Generic;

namespace Edu_Nexus.Application.Features.CareerTracks.DTOs;

public class CareerTrackDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CareerTrackJdDto> Jds { get; set; } = new();
}
