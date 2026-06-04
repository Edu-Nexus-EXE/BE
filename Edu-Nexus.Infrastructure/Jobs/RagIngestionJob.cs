using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Edu_Nexus.Application.Interfaces.Storage;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.RagDocuments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Edu_Nexus.Infrastructure.Jobs;

/// Splits an uploaded RAG document into text chunks ready for embedding.
/// Phase 1 stops after chunking; the chunks land in rag_chunks with NULL
/// embedding vectors. Once the embedding pipeline is wired the same job
/// (or a follow-up) populates the Embedding column.
public class RagIngestionJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RagIngestionJob> _logger;

    public RagIngestionJob(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IPdfTextExtractor pdfExtractor,
        IConfiguration configuration,
        ILogger<RagIngestionJob> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _pdfExtractor = pdfExtractor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync(Guid ragDocumentId, CancellationToken cancellationToken)
    {
        var doc = await _unitOfWork.RagDocuments.FirstOrDefaultAsync(
            d => d.Id == ragDocumentId, "", cancellationToken);

        if (doc == null)
        {
            _logger.LogWarning("RagIngestionJob: document {Id} not found", ragDocumentId);
            return;
        }

        try
        {
            doc.EmbeddingStatus = EmbeddingStatus.Processing;
            _unitOfWork.RagDocuments.Update(doc);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            string fullText;
            await using (var stream = await _fileStorage.OpenReadAsync(doc.FileUrl!, cancellationToken))
            {
                fullText = _pdfExtractor.Extract(stream);
            }

            var chunkSize = _configuration.GetValue<int?>("Rag:ChunkSize") ?? 800;
            var chunkOverlap = _configuration.GetValue<int?>("Rag:ChunkOverlap") ?? 100;

            var chunks = Chunk(fullText, chunkSize, chunkOverlap).ToList();

            for (var i = 0; i < chunks.Count; i++)
            {
                _unitOfWork.RagChunks.Add(new RagChunk
                {
                    DocumentId = doc.Id,
                    ChunkIndex = i,
                    Content = chunks[i],
                    TokenCount = EstimateTokenCount(chunks[i]),
                    Embedding = null,
                });
            }

            doc.ChunksCount = chunks.Count;
            doc.EmbeddingStatus = EmbeddingStatus.Completed;
            _unitOfWork.RagDocuments.Update(doc);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("RagIngestionJob completed for {Id}, {Chunks} chunks", ragDocumentId, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RagIngestionJob failed for {Id}", ragDocumentId);
            doc.EmbeddingStatus = EmbeddingStatus.Failed;
            _unitOfWork.RagDocuments.Update(doc);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static IEnumerable<string> Chunk(string text, int targetSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        if (targetSize <= 0) targetSize = 800;
        if (overlap < 0 || overlap >= targetSize) overlap = Math.Min(100, targetSize / 4);

        var normalized = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        if (normalized.Length == 0) yield break;

        var step = targetSize - overlap;
        for (var i = 0; i < normalized.Length; i += step)
        {
            var end = Math.Min(i + targetSize, normalized.Length);
            yield return normalized.Substring(i, end - i);
            if (end == normalized.Length) yield break;
        }
    }

    private static int EstimateTokenCount(string content)
        => Math.Max(1, content.Length / 4);
}
