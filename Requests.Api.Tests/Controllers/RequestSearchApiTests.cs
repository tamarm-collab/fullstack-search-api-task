using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Requests.Application.Requests;
using Xunit;

namespace Requests.Api.Tests.Controllers;

/// <summary>
/// Integration tests for the Request Search API endpoint.
/// Uses WebApplicationFactory to test the real HTTP request/response cycle.
/// 
/// Test Categories:
/// 1. Happy Path - Valid requests return expected results
/// 2. Validation - Invalid input returns 400 Bad Request
/// 3. Authorization - Regular users see only their data, admins see all
/// 4. Filtering - Date range filter works correctly
/// 5. Sorting - Results can be sorted ascending/descending
/// </summary>
public class RequestSearchApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public RequestSearchApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Creates an HTTP client with specified authentication headers.
    /// </summary>
    private HttpClient CreateClient(int userId = 1, bool isAdmin = true)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Is-Admin", isAdmin.ToString().ToLower());
        return client;
    }

    #region Happy Path Tests

    /// <summary>
    /// Valid query should return 200 OK with correct PagedResult structure.
    /// Validates the API returns well-formed response with pagination metadata.
    /// </summary>
    [Fact]
    public async Task Search_WithValidQuery_ReturnsOkWithPagedResult()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/requests/search?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<RequestDto>>(content, _jsonOptions);
        
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(0);
        result.TotalPages.Should().BeGreaterOrEqualTo(0);
    }

    /// <summary>
    /// Query with no matching results should return empty PagedResult with 200 OK.
    /// The API should NOT return 404 for empty results - this is valid behavior.
    /// </summary>
    [Fact]
    public async Task Search_WithNoMatchingResults_ReturnsEmptyPagedResult()
    {
        // Arrange
        var client = CreateClient();

        // Act - search for a request number that doesn't exist
        var response = await client.GetAsync(
            "/api/requests/search?pageNumber=1&pageSize=10&requestNumber=NONEXISTENT-REQUEST-12345678");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "no matching results should return 200 OK, not 404");
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<RequestDto>>(content, _jsonOptions);
        
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Invalid pagination parameters should return 400 Bad Request.
    /// Tests: pageNumber=0, pageSize=0, negative values.
    /// </summary>
    [Theory]
    [InlineData(0, 10, "pageNumber=0")]
    [InlineData(1, 0, "pageSize=0")]
    [InlineData(-1, 10, "pageNumber=-1")]
    public async Task Search_WithInvalidPaginationParams_ReturnsBadRequest(
        int pageNumber, int pageSize, string scenario)
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync(
            $"/api/requests/search?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"scenario '{scenario}' should return 400 Bad Request");
    }

    /// <summary>
    /// DateFrom > DateTo should return 400 Bad Request.
    /// This is a business rule validation - date range must be logical.
    /// </summary>
    [Fact]
    public async Task Search_WithInvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var client = CreateClient();
        var dateFrom = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc).ToString("o");
        var dateTo = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString("o");
        
        // Act
        var response = await client.GetAsync(
            $"/api/requests/search?pageNumber=1&pageSize=10&dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "DateFrom > DateTo should return 400 Bad Request");
    }

    #endregion

    #region Authorization Tests

    /// <summary>
    /// Regular (non-admin) users should see fewer requests than admins.
    /// Regular users can only see requests they own or are assigned to.
    /// We compare TotalCount (not Items.Count) to check actual filtered totals.
    /// </summary>
    [Fact]
    public async Task Search_AsRegularUser_ShouldSeeLessThanAdmin()
    {
        // Arrange
        var adminClient = CreateClient(userId: 1, isAdmin: true);
        var regularClient = CreateClient(userId: 1, isAdmin: false);

        // Act - get results as admin (pageSize max is 100, but we check TotalCount)
        var adminResponse = await adminClient.GetAsync("/api/requests/search?pageNumber=1&pageSize=100");
        var adminContent = await adminResponse.Content.ReadAsStringAsync();
        var adminResult = JsonSerializer.Deserialize<PagedResult<RequestDto>>(adminContent, _jsonOptions);

        // Act - get results as regular user
        var regularResponse = await regularClient.GetAsync("/api/requests/search?pageNumber=1&pageSize=100");
        var regularContent = await regularResponse.Content.ReadAsStringAsync();
        var regularResult = JsonSerializer.Deserialize<PagedResult<RequestDto>>(regularContent, _jsonOptions);

        // Assert
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        regularResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        adminResult.Should().NotBeNull();
        regularResult.Should().NotBeNull();
        
        // Regular user should see equal or fewer requests than admin
        // We compare TotalCount which reflects ALL matching records, not just the page
        regularResult!.TotalCount.Should().BeLessOrEqualTo(adminResult!.TotalCount,
            "regular user should see a subset of what admin sees");
    }

    #endregion

    #region Filtering Tests

    /// <summary>
    /// Date range filtering should return only requests within the specified range.
    /// All returned items must have CreatedAt between DateFrom and DateTo.
    /// </summary>
    [Fact]
    public async Task Search_WithDateRangeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var client = CreateClient();
        var today = DateTime.UtcNow.Date;
        var dateFrom = today.AddDays(-30).ToString("o");
        var dateTo = today.AddDays(1).ToString("o");
        
        // Act
        var response = await client.GetAsync(
            $"/api/requests/search?pageNumber=1&pageSize=100&dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<RequestDto>>(content, _jsonOptions);
        
        result.Should().NotBeNull();
        
        // All returned items should be within the date range
        foreach (var item in result!.Items)
        {
            item.CreatedAt.Should().BeOnOrAfter(today.AddDays(-30),
                "all items should have CreatedAt >= DateFrom");
            item.CreatedAt.Should().BeOnOrBefore(today.AddDays(1),
                "all items should have CreatedAt <= DateTo");
        }
    }

    #endregion

    #region Sorting Tests

    /// <summary>
    /// Sorting by CreatedAt should return results in correct order.
    /// Tests both ascending (oldest first) and descending (newest first).
    /// </summary>
    [Theory]
    [InlineData("asc", true)]
    [InlineData("desc", false)]
    public async Task Search_WithSorting_ReturnsOrderedResults(string sortDirection, bool isAscending)
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync(
            $"/api/requests/search?pageNumber=1&pageSize=50&sortBy=createdAt&sortDirection={sortDirection}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<RequestDto>>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();

        if (result.Items.Count > 1)
        {
            for (int i = 1; i < result.Items.Count; i++)
            {
                if (isAscending)
                {
                    result.Items[i].CreatedAt.Should().BeOnOrAfter(result.Items[i - 1].CreatedAt,
                        $"item {i} should have CreatedAt >= item {i - 1} when sorted ascending");
                }
                else
                {
                    result.Items[i].CreatedAt.Should().BeOnOrBefore(result.Items[i - 1].CreatedAt,
                        $"item {i} should have CreatedAt <= item {i - 1} when sorted descending");
                }
            }
        }
    }

    #endregion
}
