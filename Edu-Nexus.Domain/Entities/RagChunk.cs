using System;
using System.Collections.Generic;
using Pgvector;

namespace Edu_Nexus.Domain.Entities;

public partial class RagChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = null!;

    public Vector? Embedding { get; set; }

    public int? TokenCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual RagDocument Document { get; set; } = null!;
}
