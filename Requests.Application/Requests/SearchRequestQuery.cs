using Requests.Domain.Entities;

namespace Requests.Application.Requests;

/// <summary>
/// Data Transfer Object for search query parameters
/// </summary>
public sealed record SearchRequestQuery
{
    /// <summary>
    /// Partial request number search (case-insensitive contains)
    /// </summary>
    public string? RequestNumber { get; init; }

    /// <summary>
    /// Filter by one or more statuses (OR logic)
    /// </summary>
    public List<RequestStatus>? Statuses { get; init; }

    /// <summary>
    /// Filter by request type
    /// </summary>
    public RequestType? RequestType { get; init; }

    /// <summary>
    /// Start date filter (inclusive, UTC)
    /// </summary>
    public DateTime? DateFrom { get; init; }

    /// <summary>
    /// End date filter (inclusive, UTC)
    /// </summary>
    public DateTime? DateTo { get; init; }

    /// <summary>
    /// Sort field name (must be in whitelist)
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction: "asc" or "desc"
    /// </summary>
    public string? SortDirection { get; init; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Page size (1-100)
    /// </summary>
    public int PageSize { get; init; } = 20;
}
