using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public interface IRequestRepository
{
    /// <summary>
    /// Search requests with server-side filtering, sorting, and pagination.
    /// Filtering is done inside the repository using simple IQueryable chaining
    /// to ensure EF Core can translate all predicates to SQL.
    /// </summary>
    Task<PagedResult<Request>> SearchAsync(
        SearchRequestQuery query,
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}
