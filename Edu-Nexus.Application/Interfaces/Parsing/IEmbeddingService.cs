namespace Edu_Nexus.Application.Interfaces.Parsing;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken ct = default);
    Task<IList<ReadOnlyMemory<float>>> EmbedBatchAsync(IList<string> texts, CancellationToken ct = default);
}
