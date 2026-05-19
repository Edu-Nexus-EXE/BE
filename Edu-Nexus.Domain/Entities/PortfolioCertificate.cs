using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class PortfolioCertificate
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Issuer { get; set; }

    public DateOnly? IssuedDate { get; set; }

    public DateOnly? ExpiresDate { get; set; }

    public string? CredentialUrl { get; set; }

    public string? FileUrl { get; set; }

    public bool IsVisible { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
