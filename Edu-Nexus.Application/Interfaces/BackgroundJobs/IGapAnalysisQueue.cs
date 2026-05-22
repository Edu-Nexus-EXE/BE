namespace Edu_Nexus.Application.Interfaces.BackgroundJobs;

public interface IGapAnalysisQueue
{
    void Enqueue(Guid gapAnalysisId);
}
