using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Hangfire;

namespace Edu_Nexus.Infrastructure.BackgroundJobs;

public class RoadmapGenerateQueue : IRoadmapGenerateQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public RoadmapGenerateQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid roadmapId)
    {
        _backgroundJobClient.Enqueue<Edu_Nexus.Infrastructure.Jobs.RoadmapGenerateJob>(x => x.ExecuteAsync(roadmapId, CancellationToken.None));
    }
}
