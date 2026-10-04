using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Requests.Application.Requests;
using Requests.Domain.Entities;
using Requests.Infrastructure.Persistence;

namespace Requests.Infrastructure.Repositories;

public sealed class RequestRepository : IRequestRepository
{
    private readonly RequestsDbContext _db;

    /// <summary>
    /// Sort field mapping for security - whitelist of allowed sort fields.
    /// Uses case-insensitive comparison to handle user input.
    /// Exposed as static readonly for single source of truth (used by Validator).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Expression<Func<Request, object>>> SortMappings =
        new Dictionary<string, Expression<Func<Request, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = r => r.Id,
            ["RequestNumber"] = r => r.RequestNumber,
            ["Status"] = r => r.Status,
            ["RequestType"] = r => r.RequestType,
            ["CreatedAt"] = r => r.CreatedAt,
            ["CustomerId"] = r => r.CustomerId,
            ["OwnerId"] = r => r.OwnerId
        };

    /// <summary>
    /// Allowed sort field names (exposed for Validator - single source of truth).
    /// </summary>
    public static IEnumerable<string> AllowedSortFields => SortMappings.Keys;

    public RequestRepository(RequestsDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Search requests with server-side filtering, sorting, and pagination.
    /// Uses simple IQueryable chaining (.Where()) instead of Expression.Invoke 
    /// to ensure EF Core can translate all predicates to SQL.
    /// </summary>
    public async Task<PagedResult<Request>> SearchAsync(
        SearchRequestQuery query,
        int currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        // Start with base query - AsNoTracking for read-only performance
        IQueryable<Request> queryable = _db.Requests.AsNoTracking();

        // Authorization filter - regular users can only see their own requests
        if (!isAdministrator)
        {
            queryable = queryable.Where(r => r.OwnerId == currentUserId || r.AssignedToUserId == currentUserId);
        }

        // Request number partial search (case-insensitive via DB collation)
        if (!string.IsNullOrWhiteSpace(query.RequestNumber))
        {
            var searchTerm = query.RequestNumber.Trim();
            queryable = queryable.Where(r => r.RequestNumber.Contains(searchTerm));
        }

        // Status filtering (OR logic via Contains)
        if (query.Statuses != null && query.Statuses.Count > 0)
        {
            queryable = queryable.Where(r => query.Statuses.Contains(r.Status));
        }

        // Request type filtering
        if (query.RequestType.HasValue)
        {
            queryable = queryable.Where(r => r.RequestType == query.RequestType.Value);
        }

        // Date range filtering - DateFrom (inclusive)
        if (query.DateFrom.HasValue)
        {
            queryable = queryable.Where(r => r.CreatedAt >= query.DateFrom.Value);
        }

        // Date range filtering - DateTo (inclusive, end of day)
        if (query.DateTo.HasValue)
        {
            // Include the entire day by comparing to end of day (23:59:59.999)
            var endOfDay = query.DateTo.Value.Date.AddDays(1).AddTicks(-1);
            queryable = queryable.Where(r => r.CreatedAt <= endOfDay);
        }

        // Get total count (single DB call)
        var totalCount = await queryable.CountAsync(cancellationToken);

        // Apply sorting using whitelist mapping
        var sortDescending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                            || string.IsNullOrEmpty(query.SortDirection);
        queryable = ApplySorting(queryable, query.SortBy, sortDescending);

        // Apply pagination
        var pageNumber = Math.Max(1, query.PageNumber);
        var skip = (pageNumber - 1) * query.PageSize;
        var items = await queryable
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Request>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = query.PageSize
        };
    }

    /// <summary>
    /// Apply sorting to query using safe whitelist validation.
    /// Defaults to CreatedAt descending if sort field is not specified or invalid.
    /// IMPORTANT: Always adds ThenBy(Id) as tie-breaker for deterministic pagination.
    /// Without this, sorting by non-unique fields (Status, RequestType) can cause
    /// duplicate or missing records across pages.
    /// </summary>
    private static IQueryable<Request> ApplySorting(
        IQueryable<Request> query,
        string? sortBy,
        bool sortDescending)
    {
        // Default sort if not specified or invalid
        if (string.IsNullOrEmpty(sortBy) || !SortMappings.ContainsKey(sortBy))
        {
            // Always add Id as tie-breaker for deterministic pagination
            return query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);
        }

        var sortExpression = SortMappings[sortBy];

        // Always add ThenBy(Id) as tie-breaker for deterministic pagination
        return sortDescending
            ? query.OrderByDescending(sortExpression).ThenByDescending(r => r.Id)
            : query.OrderBy(sortExpression).ThenBy(r => r.Id);
    }
}
