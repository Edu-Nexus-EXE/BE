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
    IRepository<AssessmentSession> AssessmentSessions { get; }
    IRepository<AssessmentQuestion> AssessmentQuestions { get; }
    IRepository<AssessmentAnswer> AssessmentAnswers { get; }
    IRepository<GapAnalysisSkill> GapAnalysisSkills { get; }
    IRepository<Skill> Skills { get; }
    IRepository<RoadmapNode> RoadmapNodes { get; }
    IRepository<LearningResource> LearningResources { get; }
    IRepository<SkillResource> SkillResources { get; }
    IRepository<CareerTrack> CareerTracks { get; }
    IRepository<CareerTrackJd> CareerTrackJds { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
