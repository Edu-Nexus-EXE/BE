using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Infrastructure.Jobs;
using Hangfire;

namespace Edu_Nexus.Infrastructure.BackgroundJobs;

public class HangfireGapAnalysisQueue : IGapAnalysisQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireGapAnalysisQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid gapAnalysisId)
    {
        _backgroundJobClient.Enqueue<GapAnalysisJob>(j => j.RunAsync(gapAnalysisId, CancellationToken.None));
    }
}
