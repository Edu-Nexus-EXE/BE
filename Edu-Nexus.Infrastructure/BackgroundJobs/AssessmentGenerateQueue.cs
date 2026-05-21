using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Infrastructure.Jobs;
using Hangfire;

namespace Edu_Nexus.Infrastructure.BackgroundJobs;

public class HangfireAssessmentGenerateQueue : IAssessmentGenerateQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireAssessmentGenerateQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid sessionId)
    {
        _backgroundJobClient.Enqueue<AssessmentGenerateJob>(j => j.RunAsync(sessionId, CancellationToken.None));
    }
}
