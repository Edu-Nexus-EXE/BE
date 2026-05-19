using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class RagDocument
{
    public Guid Id { get; set; }

    public Guid? UploadedBy { get; set; }

    public string Title { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public string? FileUrl { get; set; }

    public List<Guid> RelatedSkillIds { get; set; } = null!;

    public string? Metadata { get; set; }

    public int ChunksCount { get; set; }

    public string EmbeddingStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RagChunk> RagChunks { get; set; } = new List<RagChunk>();

    public virtual User? UploadedByNavigation { get; set; }
}
