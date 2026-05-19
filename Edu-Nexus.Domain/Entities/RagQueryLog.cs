using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class RagQueryLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string QueryType { get; set; } = null!;

    public Guid? EntityId { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public decimal CostUsd { get; set; }

    public int DurationMs { get; set; }

    public string ModelUsed { get; set; } = null!;

    public bool Success { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
