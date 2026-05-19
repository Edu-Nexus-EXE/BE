using System;
using System.Collections.Generic;

namespace Edu_Nexus.Domain.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string AuthProvider { get; set; } = null!;

    public string? GoogleSub { get; set; }

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = null!;

    public bool IsBanned { get; set; }

    public bool IsSurveyCompleted { get; set; }

    public string? PortfolioUrlSlug { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<AdminAction> AdminActions { get; set; } = new List<AdminAction>();

    public virtual ICollection<AffiliateClick> AffiliateClicks { get; set; } = new List<AffiliateClick>();

    public virtual ICollection<AssessmentPath> AssessmentPaths { get; set; } = new List<AssessmentPath>();

    public virtual ICollection<AssessmentSession> AssessmentSessions { get; set; } = new List<AssessmentSession>();

    public virtual ICollection<CareerTrack> CareerTracks { get; set; } = new List<CareerTrack>();

    public virtual ICollection<CvSubmission> CvSubmissions { get; set; } = new List<CvSubmission>();

    public virtual ICollection<GapAnalysis> GapAnalyses { get; set; } = new List<GapAnalysis>();

    public virtual ICollection<JdSubmission> JdSubmissions { get; set; } = new List<JdSubmission>();

    public virtual OnboardingResponse? OnboardingResponse { get; set; }

    public virtual ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();

    public virtual Portfolio? Portfolio { get; set; }

    public virtual ICollection<PortfolioCertificate> PortfolioCertificates { get; set; } = new List<PortfolioCertificate>();

    public virtual ICollection<PortfolioProject> PortfolioProjects { get; set; } = new List<PortfolioProject>();

    public virtual ICollection<RagDocument> RagDocuments { get; set; } = new List<RagDocument>();

    public virtual ICollection<RagQueryLog> RagQueryLogs { get; set; } = new List<RagQueryLog>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

    public virtual ICollection<SubscriptionRenewalNotification> SubscriptionRenewalNotifications { get; set; } = new List<SubscriptionRenewalNotification>();

    public virtual UserSubscription? UserSubscription { get; set; }
}
