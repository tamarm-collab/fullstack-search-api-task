# Implementation Plan: Request Search & Filter Feature

## Overview

This implementation plan breaks down the search and filter feature into discrete coding tasks. The feature adds server-side filtering using IQueryable, pagination, sorting, validation with FluentValidation, and role-based authorization at the database query level. Tasks are ordered to build incrementally with working code at each checkpoint.

**Language:** C# (.NET 8) for backend, TypeScript (Angular 18+) for optional frontend

## Tasks

- [x] 1. Add Core DTOs and Interfaces
  - [x] 1.1 Create SearchRequestQuery DTO in Requests.Application
    - Add `SearchRequestQuery.cs` with properties: RequestNumber, Statuses (List<RequestStatus>), RequestType, DateFrom, DateTo, SortBy, SortDirection, PageNumber, PageSize
    - Include default values: PageNumber=1, PageSize=20
    - _Requirements: 2.1, 3.1, 4.1, 5.1, 6.1, 7.1_
  
  - [x] 1.2 Create PagedResult<T> generic wrapper in Requests.Application
    - Add `PagedResult.cs` with properties: Items, TotalCount, PageNumber, PageSize
    - Add computed properties: TotalPages, HasNextPage, HasPreviousPage
    - _Requirements: 7.2, 7.3, 7.4, 14.1_

  - [x] 1.3 Update IRequestRepository interface with SearchAsync method
    - Add method signature: `Task<PagedResult<Request>> SearchAsync(Expression<Func<Request, bool>>? filter, string? sortBy, bool sortDescending, int pageNumber, int pageSize, CancellationToken cancellationToken)`
    - Keep existing GetAllAsync method
    - _Requirements: 1.1, 1.2_

  - [x] 1.4 Update IRequestService interface with SearchAsync method
    - Add method signature: `Task<PagedResult<RequestDto>> SearchAsync(SearchRequestQuery query, int currentUserId, bool isAdministrator, CancellationToken cancellationToken)`
    - Keep existing GetRequestsAsync method
    - _Requirements: 1.1, 8.1, 9.1_

- [x] 2. Checkpoint - Build passes with new interfaces
  - Ensure solution builds successfully with the new DTOs and interfaces
  - Verify no breaking changes to existing functionality

- [x] 3. Implement Repository Layer with IQueryable
  - [x] 3.1 Implement SearchAsync in RequestRepository
    - Add sort field whitelist mapping (Dictionary<string, Expression>)
    - Build IQueryable with filter expression
    - Use AsNoTracking() for read-only performance
    - Execute count query and data query
    - Apply sorting with safe whitelist validation
    - **Add ThenBy(r => r.Id) as tie-breaker for deterministic pagination**
    - Apply Skip/Take pagination
    - Return PagedResult<Request>
    - _Requirements: 1.1, 1.2, 1.3, 6.1, 6.2, 6.3, 6.4, 6.7, 15.1, 15.3, 15.4_

- [x] 4. Implement Service Layer with Authorization
  - [x] 4.1 Implement BuildFilterExpression in RequestService
    - Add authorization predicate for non-admin users (OwnerId == userId OR AssignedToUserId == userId)
    - Add RequestNumber partial match filter (case-insensitive Contains)
    - Add Statuses filter with Contains (OR logic)
    - Add RequestType equality filter
    - Add DateFrom/DateTo range filters with **DateTo extended to end of day** (Date.AddDays(1).AddTicks(-1))
    - Combine all predicates with AND logic using **ExpressionVisitor** (NOT Expression.Invoke - EF Core cannot translate it)
    - _Requirements: 2.1, 2.2, 3.1, 3.2, 4.1, 4.2, 4.3, 4.6, 5.1, 8.1, 8.2, 9.1, 1.5_

  - [x] 4.2 Implement SearchAsync in RequestService
    - Call BuildFilterExpression with query parameters and user context
    - Determine sort direction from query.SortDirection
    - Normalize page number (Math.Max(1, query.PageNumber))
    - Call repository SearchAsync
    - Map Request entities to RequestDto
    - Return PagedResult<RequestDto>
    - _Requirements: 1.1, 1.3, 8.1, 9.1_

- [x] 5. Checkpoint - Service and Repository integration
  - Ensure solution builds successfully
  - Manually verify method signatures are correct

- [x] 6. Add FluentValidation
  - [x] 6.1 Add FluentValidation NuGet package to Requests.Api project
    - Add FluentValidation.AspNetCore package
    - _Requirements: 10.1_

  - [x] 6.2 Create SearchRequestQueryValidator in Requests.Api
    - Add `Validators/SearchRequestQueryValidator.cs`
    - Add AllowedSortFields whitelist HashSet
    - Validate PageNumber >= 1
    - Validate PageSize between 1 and 100
    - Validate SortBy against whitelist (when provided)
    - Validate SortDirection is "asc" or "desc" (when provided)
    - Validate DateFrom <= DateTo (when both provided)
    - Validate Statuses enum values
    - Validate RequestType enum value
    - _Requirements: 4.4, 6.5, 10.1, 10.2, 10.3, 10.4_

  - [x] 6.3 Register FluentValidation in Program.cs
    - Add `AddFluentValidationAutoValidation()`
    - Add `AddValidatorsFromAssemblyContaining<SearchRequestQueryValidator>()`
    - _Requirements: 10.1_

- [x] 7. Add Controller Endpoint
  - [x] 7.1 Add Search endpoint in RequestsController
    - Add `[HttpGet("search")]` endpoint
    - Accept `[FromQuery] SearchRequestQuery query`
    - Extract userId from X-User-Id header (return 401 if 0)
    - Extract isAdmin from X-Is-Admin header
    - Call service.SearchAsync
    - Return Ok(result) with PagedResult<RequestDto>
    - Add ProducesResponseType attributes for 200, 400, 401
    - _Requirements: 8.3, 8.4, 14.2, 14.3, 14.4_

  - [x] 7.2 Add CORS configuration in Program.cs
    - Add CORS policy for Angular frontend (localhost:4200)
    - Apply UseCors middleware
    - _Requirements: 14.5_

- [x] 8. Checkpoint - API endpoint working
  - Ensure solution builds and runs
  - Test endpoint manually with Swagger: GET /api/requests/search?pageNumber=1&pageSize=10
  - Verify 401 when X-User-Id header is missing or 0

- [x] 9. Add Unit Tests
  - [x] 9.1 Create RequestServiceSearchTests
    - Create test class with InMemory database setup
    - **Test 1:** Search_RegularUser_ReturnsOnlyOwnedOrAssignedRequests - verify non-admin sees only their requests
    - **Test 2:** Search_Admin_ReturnsAllMatchingRequests - verify admin sees all requests
    - **Test 3:** Search_MultipleFilters_CombinedWithAnd - verify AND logic for combined filters
    - **Test 4:** Search_RequestNumber_WithSpecialChars_NoSqlInjection - verify safe handling of special characters
    - _Requirements: 8.1, 8.2, 9.1, 9.2, 1.3, 2.3_

- [x] 10. Add Integration Tests
  - [x] 10.1 Create RequestSearchApiTests
    - Create test class using WebApplicationFactory<Program>
    - **Test 1:** Search_WithValidQuery_ReturnsOkWithPagedResult - verify 200 with correct PagedResult structure
    - **Test 2:** Search_WithNoMatchingResults_ReturnsEmptyPagedResult - verify 200 (not 404) for empty results
    - **Test 3-5:** Search_WithInvalidPaginationParams_ReturnsBadRequest - Theory with pageNumber=0, pageSize=0, pageNumber=-1
    - **Test 6:** Search_WithInvalidDateRange_ReturnsBadRequest - verify DateFrom > DateTo returns 400
    - **Test 7:** Search_AsRegularUser_ShouldSeeLessThanAdmin - verify authorization filtering
    - **Test 8:** Search_WithDateRangeFilter_ReturnsFilteredResults - verify date range filter
    - **Test 9-10:** Search_WithSorting_ReturnsOrderedResults - Theory with asc/desc
    - _Requirements: 7.5, 7.6, 6.5, 4.4, 10.1, 8.1, 9.1_

- [x] 11. Checkpoint - All tests pass
  - Run all unit and integration tests
  - Ensure all 11 test methods pass (some expand to multiple test cases via Theory)
  - Total test cases: 11 methods with Theory expanding to additional cases

- [x] 12. Documentation
  - [x] 12.1 Create Architecture Document (Part 2)
    - Document the layered architecture (Domain → Application → Infrastructure → API)
    - Explain IQueryable approach for database-level filtering
    - Document authorization strategy (filter at query level)
    - Include architecture diagram (Mermaid)
    - Explain FluentValidation integration
    - Document testing strategy and test coverage
    - _Requirements: 1.1, 1.4, 8.2, 15.1_

  - [x] 12.2 Update README
    - Add API documentation for GET /api/requests/search
    - Document query parameters with examples
    - Document response structure (PagedResult)
    - Add example curl commands
    - Document required headers (X-User-Id, X-Is-Admin)
    - _Requirements: 14.1, 14.2_

- [x] 13. Final Checkpoint - All requirements complete
  - Ensure all backend tasks are complete
  - Verify all tests pass
  - Confirm documentation is in place
  - Ask user if questions arise

- [x] 14. (Optional) Frontend - Angular Implementation
  - [x] 14.1 Create Angular models
    - Create `models/search-request-query.model.ts`
    - Create `models/paged-result.model.ts`
    - Create `models/request.model.ts` with RequestDto interface
    - Define RequestStatus and RequestType enums
    - _Requirements: 11.1_

  - [x] 14.2 Create RequestSearchService
    - Create `services/request-search.service.ts`
    - Implement search(query) method with HttpClient
    - Implement buildHttpParams to construct query string
    - Handle multiple statuses as repeated params
    - _Requirements: 11.1, 11.3_

  - [x] 14.3 Create SearchRequestsComponent
    - Create component with reactive form
    - Add form controls for all filter fields
    - Implement onSearch() to call service
    - Implement onClear() to reset form
    - Add loading$ and error$ BehaviorSubjects
    - Handle pagination with onPageChange()
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 13.1, 13.2_

  - [x] 14.4 Create RequestsTableComponent
    - Create table component with @Input for results
    - Display all columns: RequestNumber, Status, RequestType, CustomerId, OwnerId, AssignedToUserId, CreatedAt
    - Add status and type label mappings (Hebrew)
    - Format dates with toLocaleDateString('he-IL')
    - Add pagination controls
    - _Requirements: 12.1, 12.2, 12.3, 12.5, 12.6_

  - [x] 14.5 Add error handling
    - Create ErrorInterceptor for HTTP errors
    - Display validation errors from 400 responses
    - Display generic error message for 500 errors
    - Preserve filter state on error
    - _Requirements: 12.4, 13.3, 13.4, 13.5_

- [x] 15. Final Checkpoint - Complete implementation
  - All backend code complete and tested
  - All documentation in place
  - Optional frontend completed
  - Ensure all tests pass, ask the user if questions arise

## Notes

- Tasks 1-13 are the core backend implementation required for the test
- Task 14 (Frontend) is optional and depends on available time
- Tests follow the "meaningful test cases, not quantity" principle from the design (6-7 backend tests)
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation of working code
- The implementation uses .NET 8 with Entity Framework Core 8 and FluentValidation

## Critical Implementation Notes (from Code Review)

### 1. EF Core LINQ Translation
**Problem:** `Expression.Invoke` cannot be translated to SQL by EF Core LINQ Provider.
**Solution:** Use `ExpressionVisitor` with `ParameterReplacer` to combine predicates safely.

### 2. DateTo Boundary Handling
**Problem:** Sending "2026-10-04" without time component excludes records created during that day.
**Solution:** Extend `DateTo` to end of day: `DateTo.Value.Date.AddDays(1).AddTicks(-1)`.

### 3. Deterministic Pagination
**Problem:** Sorting by non-unique fields (Status, RequestType) causes duplicate/missing records across pages.
**Solution:** Always add `ThenBy(r => r.Id)` as secondary sort (tie-breaker).

### 4. Security: Header Authentication (Demo Only)
**Note:** X-User-Id and X-Is-Admin headers are acceptable for demo/testing.
**Production:** Must use JWT token with ClaimsPrincipal to prevent header forgery.

### 5. Frontend URL Sync (Optional Enhancement)
**Recommendation:** Sync filter state to URL query params via `Router.navigate` for bookmarkable/shareable searches.
