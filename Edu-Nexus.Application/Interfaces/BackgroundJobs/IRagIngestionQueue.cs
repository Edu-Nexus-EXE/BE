namespace Edu_Nexus.Application.Interfaces.BackgroundJobs;

public interface IRagIngestionQueue
{
    void Enqueue(Guid ragDocumentId);
}
