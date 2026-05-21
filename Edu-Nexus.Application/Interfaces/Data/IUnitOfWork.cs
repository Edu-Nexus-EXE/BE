using Edu_Nexus.Domain.Entities;

namespace Edu_Nexus.Application.Interfaces.Data;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<SubscriptionTier> SubscriptionTiers { get; }
    IRepository<UserSubscription> UserSubscriptions { get; }
    IRepository<OnboardingResponse> OnboardingResponses { get; }
    IRepository<JdSubmission> JdSubmissions { get; }
    IRepository<JdSkill> JdSkills { get; }
    IRepository<AssessmentPath> AssessmentPaths { get; }
    IRepository<GapAnalysis> GapAnalyses { get; }
    IRepository<CvSubmission> CvSubmissions { get; }
    IRepository<Roadmap> Roadmaps { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
