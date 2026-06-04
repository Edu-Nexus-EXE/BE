namespace Edu_Nexus.Application.DTOs;
using System;

public class AdminSkillDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Category { get; set; }
    public string? Major { get; set; }
    public string? Description { get; set; }
    public short DifficultyLevel { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PendingReviewSkillDto : AdminSkillDto
{
    public int RoadmapUsage { get; set; }
    public int JdUsage { get; set; }
    public int ResourceCount { get; set; }
}

public class CreateSkillRequest
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Category { get; set; }
    public string? Major { get; set; }
    public string? Description { get; set; }
    public short DifficultyLevel { get; set; }
}

public class UpdateSkillRequest
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Category { get; set; }
    public string? Major { get; set; }
    public string? Description { get; set; }
    public short DifficultyLevel { get; set; }
}

public class ApproveSkillRequest
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Major { get; set; }
    public short? DifficultyLevel { get; set; }
    public string? Description { get; set; }
}

public class AddPrerequisiteRequest
{
    public Guid PrerequisiteSkillId { get; set; }
}

public class ToggleSkillActiveRequest
{
    public bool IsActive { get; set; }
}

public class MergeSkillRequest
{
    public string? Reason { get; set; }
}

public class MergeSkillResponse
{
    public Guid OldSkillId { get; set; }
    public Guid NewSkillId { get; set; }
    public int MovedRoadmapNodes { get; set; }
    public int MovedJdSkills { get; set; }
    public int MovedGapSkills { get; set; }
    public int MovedResources { get; set; }
    public int MovedPrerequisites { get; set; }
}
