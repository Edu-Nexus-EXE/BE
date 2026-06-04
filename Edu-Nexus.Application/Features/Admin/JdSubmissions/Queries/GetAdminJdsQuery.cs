using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.JdSubmissions;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.JdSubmissions.Queries;

public record GetAdminJdsQuery(string? ParseStatus, int Page, int PageSize) : IRequest<PagedResult<AdminJdListItemDto>>;

public class GetAdminJdsQueryHandler : IRequestHandler<GetAdminJdsQuery, PagedResult<AdminJdListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminJdsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AdminJdListItemDto>> Handle(GetAdminJdsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        ParseStatus? statusFilter = request.ParseStatus?.ToLowerInvariant() switch
        {
            "pending" => Edu_Nexus.Domain.Enums.JdSubmissions.ParseStatus.Pending,
            "processing" => Edu_Nexus.Domain.Enums.JdSubmissions.ParseStatus.Processing,
            "completed" => Edu_Nexus.Domain.Enums.JdSubmissions.ParseStatus.Completed,
            "failed" => Edu_Nexus.Domain.Enums.JdSubmissions.ParseStatus.Failed,
            null or "" => null,
            _ => throw new Exception("422 INVALID_STATUS_FILTER")
        };

        var rows = (await _unitOfWork.JdSubmissions.FindAsync(
            j => j.DeletedAt == null && (statusFilter == null || j.ParseStatus == statusFilter),
            nameof(JdSubmission.User),
            cancellationToken))
            .OrderByDescending(j => j.CreatedAt)
            .ToList();

        var totalItems = rows.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = rows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new AdminJdListItemDto(
                j.Id,
                j.UserId,
                j.User?.Email ?? "",
                j.User?.FullName ?? "",
                j.SourceType.ToString().ToLowerInvariant(),
                j.SourceUrl,
                j.JobTitle,
                j.ParseStatus.ToString().ToLowerInvariant(),
                j.ParseError,
                j.CreatedAt,
                j.ParsedAt))
            .ToList();

        return new PagedResult<AdminJdListItemDto>(items, new PaginationDto(page, pageSize, totalItems, totalPages));
    }
}
