using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Application.Interfaces.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Edu_Nexus.Infrastructure.Data;

public class RagService : IRagService
{
    private readonly EduNexusDbContext _context;
    private readonly IEmbeddingService? _embedding;
    private readonly IConfiguration _config;
    private readonly ILogger<RagService> _logger;

    // IEmbeddingService chỉ đăng ký khi có OpenAI key. .NET DI KHÔNG honor optional ctor
    // param → resolve qua IServiceProvider.GetService (null nếu chưa đăng ký).
    public RagService(EduNexusDbContext context, IConfiguration config,
        ILogger<RagService> logger, IServiceProvider serviceProvider)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _embedding = serviceProvider.GetService<IEmbeddingService>();
    }

    public async Task<List<RagChunkResult>> SearchAsync(string query, int limit = 5,
        string[]? sourceTypes = null, double minSimilarity = 0.5, CancellationToken cancellationToken = default)
    {
        if (_embedding is null)
        {
            _logger.LogInformation("RagService.SearchAsync: embedding service unavailable, returning empty");
            return new();
        }

        var rawVec = await _embedding.EmbedAsync(query, cancellationToken);
        var queryVec = new Vector(rawVec.ToArray());

        var q = _context.RagChunks
            .Where(c => c.Embedding != null)
            .Join(_context.RagDocuments, c => c.DocumentId, d => d.Id, (c, d) => new { c, d });

        if (sourceTypes is { Length: > 0 })
        {
            var allowed = sourceTypes
                .Select(s => Enum.TryParse<Domain.Enums.RagDocuments.RagDocumentSourceType>(s, true, out var e) ? (Domain.Enums.RagDocuments.RagDocumentSourceType?)e : null)
                .Where(e => e.HasValue)
                .Select(e => e!.Value)
                .ToArray();
            if (allowed.Length > 0)
                q = q.Where(x => allowed.Contains(x.d.SourceType));
        }

        var rows = await q
            .OrderBy(x => x.c.Embedding!.CosineDistance(queryVec))
            .Take(limit)
            .Select(x => new
            {
                x.c.Id,
                x.c.DocumentId,
                x.c.Content,
                x.d.SourceType,
                x.d.Title,
                Distance = x.c.Embedding!.CosineDistance(queryVec),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(r => 1 - r.Distance >= minSimilarity)
            .Select(r => new RagChunkResult(
                r.Id, r.DocumentId, r.Content, r.SourceType.ToString(), null,
                1 - r.Distance, r.Title))
            .ToList();
    }

    public async Task<List<RagChunkResult>> SearchBySkillAsync(string skillName, int limit = 5,
        CancellationToken cancellationToken = default)
        => await SearchAsync(skillName, limit, null, 0.5, cancellationToken);
}
