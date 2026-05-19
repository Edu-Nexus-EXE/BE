using System;
using System.Collections.Generic;
using System.Net;

namespace Edu_Nexus.Domain.Entities;

public partial class AdminAction
{
    public Guid Id { get; set; }

    public Guid AdminUserId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string? Metadata { get; set; }

    public IPAddress? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User AdminUser { get; set; } = null!;
}
