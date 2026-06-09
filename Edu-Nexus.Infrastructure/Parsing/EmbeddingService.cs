using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001
namespace Edu_Nexus.Infrastructure.Parsing;

public class EmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _svc;

    public EmbeddingService(ITextEmbeddingGenerationService svc) => _svc = svc;

    public async Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken ct = default)
        => (await _svc.GenerateEmbeddingsAsync(new[] { text }, cancellationToken: ct))[0];

    public async Task<IList<ReadOnlyMemory<float>>> EmbedBatchAsync(IList<string> texts, CancellationToken ct = default)
        => await _svc.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
}
#pragma warning restore SKEXP0001
