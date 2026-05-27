using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;

namespace Edu_Nexus.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly EduNexusDbContext _context;

    public UnitOfWork(EduNexusDbContext context)
    {
        _context = context;
        Users = new Repository<User>(_context);
        RefreshTokens = new Repository<RefreshToken>(_context);
        SubscriptionTiers = new Repository<SubscriptionTier>(_context);
        UserSubscriptions = new Repository<UserSubscription>(_context);
        OnboardingResponses = new Repository<OnboardingResponse>(_context);
        JdSubmissions = new Repository<JdSubmission>(_context);
        JdSkills = new Repository<JdSkill>(_context);
        AssessmentPaths = new Repository<AssessmentPath>(_context);
        GapAnalyses = new Repository<GapAnalysis>(_context);
        CvSubmissions = new Repository<CvSubmission>(_context);
        Roadmaps = new Repository<Roadmap>(_context);
        AssessmentSessions = new Repository<AssessmentSession>(_context);
        AssessmentQuestions = new Repository<AssessmentQuestion>(_context);
        AssessmentAnswers = new Repository<AssessmentAnswer>(_context);
        GapAnalysisSkills = new Repository<GapAnalysisSkill>(_context);
        Skills = new Repository<Skill>(_context);
        RoadmapNodes = new Repository<RoadmapNode>(_context);
        LearningResources = new Repository<LearningResource>(_context);
        SkillResources = new Repository<SkillResource>(_context);
    }

    public IRepository<User> Users { get; private set; }
    public IRepository<RefreshToken> RefreshTokens { get; private set; }
    public IRepository<SubscriptionTier> SubscriptionTiers { get; private set; }
    public IRepository<UserSubscription> UserSubscriptions { get; private set; }
    public IRepository<OnboardingResponse> OnboardingResponses { get; private set; }
    public IRepository<JdSubmission> JdSubmissions { get; private set; }
    public IRepository<JdSkill> JdSkills { get; private set; }
    public IRepository<AssessmentPath> AssessmentPaths { get; private set; }
    public IRepository<GapAnalysis> GapAnalyses { get; private set; }
    public IRepository<CvSubmission> CvSubmissions { get; private set; }
    public IRepository<Roadmap> Roadmaps { get; private set; }
    public IRepository<AssessmentSession> AssessmentSessions { get; private set; }
    public IRepository<AssessmentQuestion> AssessmentQuestions { get; private set; }
    public IRepository<AssessmentAnswer> AssessmentAnswers { get; private set; }
    public IRepository<GapAnalysisSkill> GapAnalysisSkills { get; private set; }
    public IRepository<Skill> Skills { get; private set; }
    public IRepository<RoadmapNode> RoadmapNodes { get; private set; }
    public IRepository<LearningResource> LearningResources { get; private set; }
    public IRepository<SkillResource> SkillResources { get; private set; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
