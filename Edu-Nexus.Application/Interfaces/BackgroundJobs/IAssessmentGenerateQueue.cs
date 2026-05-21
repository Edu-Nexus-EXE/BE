namespace Edu_Nexus.Application.Interfaces.BackgroundJobs;

public interface IAssessmentGenerateQueue
{
    void Enqueue(Guid sessionId);
}
