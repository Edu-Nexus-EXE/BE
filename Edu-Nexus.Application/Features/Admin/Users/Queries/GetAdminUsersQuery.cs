using Edu_Nexus.Application.DTOs;
using Edu_Nexus.Application.Interfaces.Data;
using Edu_Nexus.Domain.Entities;
using Edu_Nexus.Domain.Enums.SubscriptionTiers;
using Edu_Nexus.Domain.Enums.UserSubscriptions;
using MediatR;

namespace Edu_Nexus.Application.Features.Admin.Users.Queries;

public record GetAdminUsersQuery(
    string? Search,
    string? Tier,
    bool? IsBanned,
    int Page,
    int PageSize) : IRequest<PagedResult<AdminUserListItemDto>>;

public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, PagedResult<AdminUserListItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminUsersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<AdminUserListItemDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        SubscriptionTierCode? tierFilter = request.Tier?.ToLowerInvariant() switch
        {
            "free" => SubscriptionTierCode.Free,
            "student" => SubscriptionTierCode.Student,
            null or "" => null,
            _ => throw new Exception("422 INVALID_TIER_FILTER")
        };

        var users = await _unitOfWork.Users.FindAsync(
            u => u.DeletedAt == null
                 && (request.IsBanned == null || u.IsBanned == request.IsBanned)
                 && (string.IsNullOrEmpty(request.Search)
                     || u.Email.Contains(request.Search)
                     || u.FullName.Contains(request.Search)),
            $"{nameof(User.UserSubscription)}.{nameof(UserSubscription.Tier)},{nameof(User.JdSubmissions)}",
            cancellationToken);

        var filtered = users
            .Where(u =>
                tierFilter == null
                || (u.UserSubscription != null && u.UserSubscription.Tier.TierCode == tierFilter))
            .OrderByDescending(u => u.CreatedAt)
            .ToList();

        var totalItems = filtered.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserListItemDto(
                u.Id,
                u.Email,
                u.FullName,
                u.UserSubscription?.Tier?.TierCode.ToString().ToLowerInvariant(),
                u.UserSubscription?.Status.ToString().ToLowerInvariant(),
                u.UserSubscription?.ExpiresAt,
                u.IsBanned,
                u.JdSubmissions.Count(j => j.DeletedAt == null),
                u.CreatedAt))
            .ToList();

        return new PagedResult<AdminUserListItemDto>(items, new PaginationDto(page, pageSize, totalItems, totalPages));
    }
}
