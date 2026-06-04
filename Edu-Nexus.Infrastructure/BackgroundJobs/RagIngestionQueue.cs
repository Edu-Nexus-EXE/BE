using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Infrastructure.Jobs;
using Hangfire;

namespace Edu_Nexus.Infrastructure.BackgroundJobs;

public class HangfireRagIngestionQueue : IRagIngestionQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireRagIngestionQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid ragDocumentId)
    {
        _backgroundJobClient.Enqueue<RagIngestionJob>(j => j.RunAsync(ragDocumentId, CancellationToken.None));
    }
}
