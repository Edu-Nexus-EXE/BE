using Edu_Nexus.Application.Interfaces.BackgroundJobs;
using Edu_Nexus.Infrastructure.Jobs;
using Hangfire;

namespace Edu_Nexus.Infrastructure.BackgroundJobs;

public class HangfireCvParseQueue : ICvParseQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireCvParseQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Enqueue(Guid cvSubmissionId, bool isReupload)
    {
        _backgroundJobClient.Enqueue<CvParseJob>(j => j.RunAsync(cvSubmissionId, isReupload, CancellationToken.None));
    }
}
