using Requests.Application.Requests;
using Requests.Domain.Entities;
using Xunit;

namespace Requests.Application.Tests.Requests;

/// <summary>
/// Unit tests for RequestService.SearchAsync method.
/// Uses FakeRepository to test business logic in isolation.
/// 
/// Test Categories:
/// 1. Authorization - Regular users vs Admin access control
/// 2. Filter Logic - Multiple filters combine with AND
/// 3. Pagination - Metadata calculation is correct
/// </summary>
public class RequestServiceSearchTests
{
    #region Test Data

    /// <summary>
    /// Creates a controlled set of 4 test requests with known ownership and attributes.
    /// 
    /// Request 1: OwnerId=1, AssignedTo=null,  Status=New,        Type=General
    /// Request 2: OwnerId=2, AssignedTo=1,     Status=InProgress, Type=Legal
    /// Request 3: OwnerId=3, AssignedTo=3,     Status=Completed,  Type=Payment
    /// Request 4: OwnerId=1, AssignedTo=2,     Status=New,        Type=Legal
    /// 
    /// User 1 can see: Request 1 (owns), Request 2 (assigned), Request 4 (owns) = 3 total
    /// </summary>
    private static List<Request> CreateTestRequests()
    {
        return new List<Request>
        {
            new Request
            {
                Id = 1,
                RequestNumber = "REQ-001",
                CustomerId = 100,
                OwnerId = 1,
                AssignedToUserId = null,
                Status = RequestStatus.New,
                RequestType = RequestType.General,
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            },
            new Request
            {
                Id = 2,
                RequestNumber = "REQ-002",
                CustomerId = 101,
                OwnerId = 2,
                AssignedToUserId = 1,
                Status = RequestStatus.InProgress,
                RequestType = RequestType.Legal,
                CreatedAt = new DateTime(2024, 1, 16, 11, 0, 0, DateTimeKind.Utc)
            },
            new Request
            {
                Id = 3,
                RequestNumber = "REQ-003",
                CustomerId = 102,
                OwnerId = 3,
                AssignedToUserId = 3,
                Status = RequestStatus.Completed,
                RequestType = RequestType.Payment,
                CreatedAt = new DateTime(2024, 1, 17, 12, 0, 0, DateTimeKind.Utc)
            },
            new Request
            {
                Id = 4,
                RequestNumber = "REQ-004",
                CustomerId = 103,
                OwnerId = 1,
                AssignedToUserId = 2,
                Status = RequestStatus.New,
                RequestType = RequestType.Legal,
                CreatedAt = new DateTime(2024, 1, 18, 14, 0, 0, DateTimeKind.Utc)
            }
        };
    }

    #endregion

    #region Authorization Tests

    /// <summary>
    /// Regular user should only see requests they own OR are assigned to.
    /// User 1: owns 1,4 + assigned to 2 = sees 3 requests.
    /// User 1 should NOT see request 3 (owned by user 3, assigned to user 3).
    /// </summary>
    [Fact]
    public async Task SearchAsync_WhenRegularUser_ShouldOnlySeeOwnedOrAssignedRequests()
    {
        // Arrange
        const int currentUserId = 1;
        const bool isAdmin = false;
        var repository = new FakeRequestRepository(CreateTestRequests());
        var service = new RequestService(repository);
        var query = new SearchRequestQuery { PageNumber = 1, PageSize = 20 };

        // Act
        var result = await service.SearchAsync(query, currentUserId, isAdmin);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Contains(result.Items, x => x.Id == 1); // Owned by user 1
        Assert.Contains(result.Items, x => x.Id == 2); // Assigned to user 1
        Assert.Contains(result.Items, x => x.Id == 4); // Owned by user 1
        Assert.DoesNotContain(result.Items, x => x.Id == 3); // Not accessible
    }

    /// <summary>
    /// Admin user should see ALL requests regardless of ownership.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WhenAdmin_ShouldSeeAllRequests()
    {
        // Arrange
        const int currentUserId = 1;
        const bool isAdmin = true;
        var repository = new FakeRequestRepository(CreateTestRequests());
        var service = new RequestService(repository);
        var query = new SearchRequestQuery { PageNumber = 1, PageSize = 20 };

        // Act
        var result = await service.SearchAsync(query, currentUserId, isAdmin);

        // Assert
        Assert.Equal(4, result.Items.Count);
    }

    #endregion

    #region Filter Combination Tests

    /// <summary>
    /// Multiple filters should combine with AND logic.
    /// Status=New AND RequestType=Legal should return ONLY request 4.
    /// Request 1 is New but General (doesn't match).
    /// Request 4 is New AND Legal (matches!).
    /// </summary>
    [Fact]
    public async Task SearchAsync_WhenMultipleFiltersProvided_ShouldCombineWithAndLogic()
    {
        // Arrange
        var repository = new FakeRequestRepository(CreateTestRequests());
        var service = new RequestService(repository);
        var query = new SearchRequestQuery
        {
            Statuses = new List<RequestStatus> { RequestStatus.New },
            RequestType = RequestType.Legal,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await service.SearchAsync(query, currentUserId: 1, isAdministrator: true);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(4, result.Items.First().Id);
        Assert.Equal("REQ-004", result.Items.First().RequestNumber);
    }

    #endregion

    #region Pagination Tests

    /// <summary>
    /// Pagination metadata should be calculated correctly.
    /// With 25 records and pageSize=10, page 2 should have:
    /// - TotalCount = 25
    /// - TotalPages = 3 (ceiling of 25/10)
    /// - HasNextPage = true (page 3 exists)
    /// - HasPreviousPage = true (page 1 exists)
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldReturnCorrectPaginationMetadata()
    {
        // Arrange
        const int totalRecords = 25;
        const int pageSize = 10;
        const int pageNumber = 2;

        var allRequests = Enumerable.Range(1, totalRecords)
            .Select(i => new Request
            {
                Id = i,
                RequestNumber = $"REQ-{i:D3}",
                CustomerId = 100 + i,
                OwnerId = 1,
                Status = RequestStatus.New,
                RequestType = RequestType.General,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();

        var repository = new FakeRequestRepository(allRequests);
        var service = new RequestService(repository);
        var query = new SearchRequestQuery { PageNumber = pageNumber, PageSize = pageSize };

        // Act
        var result = await service.SearchAsync(query, currentUserId: 1, isAdministrator: true);

        // Assert
        Assert.Equal(totalRecords, result.TotalCount);
        Assert.Equal(3, result.TotalPages); // ceil(25/10) = 3
        Assert.Equal(pageNumber, result.PageNumber);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(pageSize, result.Items.Count); // Page 2 has 10 items
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    #endregion

    #region Fake Repository

    /// <summary>
    /// In-memory implementation of IRequestRepository for unit testing.
    /// Replicates the real repository's filtering, sorting, and pagination logic.
    /// </summary>
    private sealed class FakeRequestRepository : IRequestRepository
    {
        private readonly List<Request> _requests;

        public FakeRequestRepository(List<Request> requests)
        {
            _requests = requests;
        }

        public Task<PagedResult<Request>> SearchAsync(
            SearchRequestQuery query,
            int currentUserId,
            bool isAdministrator,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Request> queryable = _requests.AsQueryable();

            // Authorization filter
            if (!isAdministrator)
            {
                queryable = queryable.Where(r => 
                    r.OwnerId == currentUserId || r.AssignedToUserId == currentUserId);
            }

            // Request number partial search
            if (!string.IsNullOrWhiteSpace(query.RequestNumber))
            {
                var searchTerm = query.RequestNumber.Trim();
                queryable = queryable.Where(r => r.RequestNumber.Contains(searchTerm));
            }

            // Status filtering
            if (query.Statuses != null && query.Statuses.Count > 0)
            {
                queryable = queryable.Where(r => query.Statuses.Contains(r.Status));
            }

            // Request type filtering
            if (query.RequestType.HasValue)
            {
                queryable = queryable.Where(r => r.RequestType == query.RequestType.Value);
            }

            // Date range filtering
            if (query.DateFrom.HasValue)
            {
                queryable = queryable.Where(r => r.CreatedAt >= query.DateFrom.Value);
            }

            if (query.DateTo.HasValue)
            {
                var endOfDay = query.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                queryable = queryable.Where(r => r.CreatedAt <= endOfDay);
            }

            var totalCount = queryable.Count();

            // Apply sorting
            var sortDescending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                                || string.IsNullOrEmpty(query.SortDirection);
            queryable = ApplySorting(queryable, query.SortBy, sortDescending);

            // Apply pagination
            var pageNumber = Math.Max(1, query.PageNumber);
            var skip = (pageNumber - 1) * query.PageSize;
            var items = queryable.Skip(skip).Take(query.PageSize).ToList();

            return Task.FromResult(new PagedResult<Request>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = query.PageSize
            });
        }

        private static IQueryable<Request> ApplySorting(
            IQueryable<Request> query, 
            string? sortBy, 
            bool sortDescending)
        {
            return sortBy?.ToLowerInvariant() switch
            {
                "id" => sortDescending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id),
                "requestnumber" => sortDescending ? query.OrderByDescending(r => r.RequestNumber) : query.OrderBy(r => r.RequestNumber),
                "status" => sortDescending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                "requesttype" => sortDescending ? query.OrderByDescending(r => r.RequestType) : query.OrderBy(r => r.RequestType),
                "customerid" => sortDescending ? query.OrderByDescending(r => r.CustomerId) : query.OrderBy(r => r.CustomerId),
                "ownerid" => sortDescending ? query.OrderByDescending(r => r.OwnerId) : query.OrderBy(r => r.OwnerId),
                _ => sortDescending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
            };
        }
    }

    #endregion
}
