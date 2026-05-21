namespace Edu_Nexus.Application.Interfaces.BackgroundJobs;

public interface IJdParseQueue
{
    void Enqueue(Guid jdSubmissionId);
}
