using Requests.Domain.Entities;

namespace Requests.Application.Requests;

public sealed class RequestService : IRequestService
{
    private readonly IRequestRepository _repository;

    public RequestService(IRequestRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Search requests with filtering, authorization, sorting, and pagination.
    /// All filtering is done at the database level via IQueryable in the repository.
    /// </summary>
    public async Task<PagedResult<RequestDto>> SearchAsync(
        SearchRequestQuery query,
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        // Execute search through repository (all filtering done at DB level)
        var result = await _repository.SearchAsync(
            query,
            currentUserId,
            isAdministrator,
            cancellationToken);
        
        // Map Request entities to RequestDto
        return new PagedResult<RequestDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    /// <summary>
    /// Maps a Request entity to a RequestDto.
    /// </summary>
    private static RequestDto MapToDto(Request request) => new(
        request.Id,
        request.RequestNumber,
        request.CustomerId,
        request.OwnerId,
        request.AssignedToUserId,
        request.Status,
        request.RequestType,
        request.CreatedAt);
}
