using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.RagDocuments.Queries;

public record GetAdminRagDocumentsQuery(int Page, int PageSize) : IRequest<PagedResult<AdminRagDocumentDto>>;

public class GetAdminRagDocumentsQueryHandler : IRequestHandler<GetAdminRagDocumentsQuery, PagedResult<AdminRagDocumentDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminRagDocumentsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AdminRagDocumentDto>> Handle(GetAdminRagDocumentsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var rows = (await _unitOfWork.RagDocuments.GetAllAsync("", cancellationToken))
            .OrderByDescending(d => d.CreatedAt)
            .ToList();

        var totalItems = rows.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new AdminRagDocumentDto(
                d.Id,
                d.Title,
                SourceTypeLabel(d.SourceType),
                d.FileUrl,
                d.RelatedSkillIds ?? new List<Guid>(),
                d.ChunksCount,
                d.EmbeddingStatus.ToString().ToLowerInvariant(),
                d.CreatedAt))
            .ToList();

        return new PagedResult<AdminRagDocumentDto>(items, new PaginationDto(page, pageSize, totalItems, totalPages));
    }

    private static string SourceTypeLabel(Edu_Nexus.Domain.Enums.RagDocuments.RagDocumentSourceType type) => type switch
    {
        Edu_Nexus.Domain.Enums.RagDocuments.RagDocumentSourceType.FptuCurriculum => "fptu_curriculum",
        Edu_Nexus.Domain.Enums.RagDocuments.RagDocumentSourceType.FptuSyllabus => "fptu_syllabus",
        _ => "external_doc",
    };
}
