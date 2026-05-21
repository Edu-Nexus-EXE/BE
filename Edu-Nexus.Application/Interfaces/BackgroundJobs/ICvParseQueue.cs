namespace Edu_Nexus.Application.Interfaces.BackgroundJobs;

public interface ICvParseQueue
{
    void Enqueue(Guid cvSubmissionId, bool isReupload);
}
