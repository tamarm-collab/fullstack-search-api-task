namespace Requests.Application.Requests;

public interface IRequestService
{
    /// <summary>
    /// Search requests with filtering, authorization, sorting, and pagination.
    /// All filtering is done at the database level via IQueryable.
    /// </summary>
    /// <param name="query">Search query parameters including filters, sorting, and pagination</param>
    /// <param name="currentUserId">The ID of the current user making the request</param>
    /// <param name="isAdministrator">Whether the current user has administrator privileges</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result containing matching requests</returns>
    Task<PagedResult<RequestDto>> SearchAsync(
        SearchRequestQuery query,
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}
