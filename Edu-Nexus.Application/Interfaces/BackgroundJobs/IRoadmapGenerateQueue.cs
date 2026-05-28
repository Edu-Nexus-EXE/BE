using System;

namespace Edu_Nexus.Application.Interfaces.BackgroundJobs;

public interface IRoadmapGenerateQueue
{
    void Enqueue(Guid roadmapId);
}
