using System;
using System.Collections.Generic;

using Edu_Nexus.Domain.Enums.RagDocuments;

namespace Edu_Nexus.Domain.Entities;

public partial class RagDocument
{
    public Guid Id { get; set; }

    public Guid? UploadedBy { get; set; }

    public string Title { get; set; } = null!;

    public RagDocumentSourceType SourceType { get; set; }

    public string? FileUrl { get; set; }

    public List<Guid> RelatedSkillIds { get; set; } = null!;

    public string? Metadata { get; set; }

    public int ChunksCount { get; set; }

    public EmbeddingStatus EmbeddingStatus { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RagChunk> RagChunks { get; set; } = new List<RagChunk>();

    public virtual User? UploadedByNavigation { get; set; }
}
