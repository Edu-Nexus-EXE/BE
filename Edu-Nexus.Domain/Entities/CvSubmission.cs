using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.JdSubmissions;

namespace Edu_Nexus.Domain.Entities;

public partial class CvSubmission
{
    public Guid Id { get; set; }

    public Guid AssessmentPathId { get; set; }

    public Guid UserId { get; set; }

    public string FileUrl { get; set; } = null!;

    public string? FileName { get; set; }

    public int? FileSizeBytes { get; set; }

    public string? MimeType { get; set; }

    public string? ParsedText { get; set; }

    public string? ParsedSkills { get; set; }

    public ParseStatus ParseStatus { get; set; }

    public string? ParseError { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ParsedAt { get; set; }

    public virtual AssessmentPath AssessmentPath { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
