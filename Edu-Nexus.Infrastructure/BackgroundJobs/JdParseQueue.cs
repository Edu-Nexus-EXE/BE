using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Infrastructure.Jobs;
using Hangfire;

namespace Edu_Nexus.Infrastructure.BackgroundJobs;

public class HangfireJdParseQueue : IJdParseQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireJdParseQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid jdSubmissionId)
    {
        _backgroundJobClient.Enqueue<JdParseJob>(j => j.RunAsync(jdSubmissionId, CancellationToken.None));
    }
}
