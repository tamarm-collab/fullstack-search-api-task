# Requirements Document

## Introduction

This document specifies the requirements for implementing a comprehensive search and filter capability for the Requests API. The existing system loads all records into memory before filtering, which is not scalable for production environments with millions of records. This feature introduces server-side filtering using IQueryable, pagination, sorting, and role-based authorization enforcement at the database query level. Additionally, an Angular frontend will provide a user-friendly interface for searching and filtering requests.

## Glossary

- **Request**: A domain entity representing a case or ticket in the system with properties including Id, RequestNumber, CustomerId, OwnerId, AssignedToUserId, Status, RequestType, CreatedAt, and UpdatedAt
- **Request_Search_API**: The backend API endpoint that accepts filter parameters and returns paginated, filtered results using IQueryable for database-level filtering
- **Search_Filter_Query**: A data transfer object containing all filter criteria (RequestNumber, Status, DateFrom, DateTo, RequestType, SortBy, SortDirection, PageNumber, PageSize)
- **Paged_Result**: A response wrapper containing the filtered items, total count, current page number, page size, and total pages for pagination support
- **Regular_User**: A non-administrator user who can only view Requests where they are the Owner (OwnerId) or the Assignee (AssignedToUserId)
- **Administrator**: A user with elevated privileges who can view all Requests regardless of ownership or assignment
- **Sort_Whitelist**: A predefined list of allowed sortable field names (Id, RequestNumber, Status, RequestType, CreatedAt, CustomerId, OwnerId) to prevent injection attacks
- **Angular_Search_Form**: The frontend component that provides input fields for all filter criteria and displays search results

## Requirements

### Requirement 1: Server-Side Filtering with IQueryable

**User Story:** As a developer, I want the search to use IQueryable for database-level filtering, so that the system can handle millions of records efficiently without loading everything into memory.

#### Acceptance Criteria

1. WHEN a search request is received, THE Request_Search_API SHALL build an IQueryable expression tree and execute filtering at the database level
2. THE Request_Search_API SHALL NOT load all records into memory before applying filters
3. WHEN multiple filter parameters are provided, THE Request_Search_API SHALL combine them using AND logic in a single database query
4. FOR ALL filter operations, THE Request_Search_API SHALL generate SQL queries that can be verified in EF Core logs
5. WHEN combining multiple filter predicates, THE Request_Search_API SHALL use ExpressionVisitor for parameter replacement instead of Expression.Invoke (which EF Core LINQ Provider cannot translate to SQL)

### Requirement 2: Request Number Partial Search

**User Story:** As a user, I want to search for requests by partial request number, so that I can find requests without knowing the exact full number.

#### Acceptance Criteria

1. WHEN a RequestNumber filter is provided, THE Request_Search_API SHALL return all Requests where the RequestNumber contains the provided value (case-insensitive)
2. WHEN the RequestNumber filter is empty or null, THE Request_Search_API SHALL not apply any RequestNumber filtering
3. WHEN the RequestNumber filter contains special characters, THE Request_Search_API SHALL escape them properly to prevent SQL injection

### Requirement 3: Status Filtering

**User Story:** As a user, I want to filter requests by one or more statuses, so that I can view only requests in specific workflow states.

#### Acceptance Criteria

1. WHEN a single Status value is provided, THE Request_Search_API SHALL return only Requests matching that status
2. WHEN multiple Status values are provided as an array, THE Request_Search_API SHALL return Requests matching any of the provided statuses (OR logic)
3. WHEN an invalid Status value is provided, THE Request_Search_API SHALL return a 400 Bad Request response with a descriptive error message
4. WHEN no Status filter is provided, THE Request_Search_API SHALL return Requests of all statuses

### Requirement 4: Date Range Filtering

**User Story:** As a user, I want to filter requests by creation date range, so that I can find requests created within a specific time period.

#### Acceptance Criteria

1. WHEN a DateFrom value is provided, THE Request_Search_API SHALL return only Requests with CreatedAt greater than or equal to DateFrom
2. WHEN a DateTo value is provided, THE Request_Search_API SHALL extend DateTo to end of day (23:59:59.9999999) to include all Requests created on that date
3. WHEN both DateFrom and DateTo are provided, THE Request_Search_API SHALL return only Requests within the date range (inclusive of both full days)
4. IF DateFrom is greater than DateTo, THEN THE Request_Search_API SHALL return a 400 Bad Request response with a descriptive error message
5. WHEN date values are provided, THE Request_Search_API SHALL interpret them as UTC dates
6. WHEN DateTo is provided without a time component, THE Request_Search_API SHALL NOT exclude Requests created during the DateTo day (e.g., sending "2026-10-04" must include Requests created at 14:30 on that day)

### Requirement 5: Request Type Filtering

**User Story:** As a user, I want to filter requests by request type, so that I can view only requests of a specific category.

#### Acceptance Criteria

1. WHEN a RequestType value is provided, THE Request_Search_API SHALL return only Requests matching that type
2. WHEN an invalid RequestType value is provided, THE Request_Search_API SHALL return a 400 Bad Request response with a descriptive error message
3. WHEN no RequestType filter is provided, THE Request_Search_API SHALL return Requests of all types

### Requirement 6: Sorting

**User Story:** As a user, I want to sort search results by various fields, so that I can organize the results in a meaningful order.

#### Acceptance Criteria

1. WHEN a SortBy parameter is provided with a valid field name from the Sort_Whitelist, THE Request_Search_API SHALL order results by that field
2. WHEN a SortDirection parameter is provided as "asc", THE Request_Search_API SHALL order results in ascending order
3. WHEN a SortDirection parameter is provided as "desc", THE Request_Search_API SHALL order results in descending order
4. WHEN no SortBy parameter is provided, THE Request_Search_API SHALL default to ordering by CreatedAt descending
5. IF a SortBy value is provided that is not in the Sort_Whitelist, THEN THE Request_Search_API SHALL return a 400 Bad Request response with a descriptive error message listing allowed fields
6. THE Sort_Whitelist SHALL contain the following fields: Id, RequestNumber, Status, RequestType, CreatedAt, CustomerId, OwnerId
7. FOR ALL sort operations, THE Request_Search_API SHALL add Id as a secondary tie-breaker to ensure deterministic pagination (preventing duplicate or missing records across pages when sorting by non-unique fields like Status or RequestType)

### Requirement 7: Pagination

**User Story:** As a user, I want search results to be paginated, so that I can navigate through large result sets efficiently.

#### Acceptance Criteria

1. WHEN PageNumber and PageSize parameters are provided, THE Request_Search_API SHALL return only the requested page of results
2. THE Paged_Result SHALL include TotalCount representing the total number of records matching the filter criteria
3. THE Paged_Result SHALL include TotalPages calculated as ceiling of (TotalCount divided by PageSize)
4. THE Paged_Result SHALL include the current PageNumber and PageSize
5. WHEN PageNumber is less than 1, THE Request_Search_API SHALL treat it as page 1
6. WHEN PageSize is less than 1 or greater than 100, THE Request_Search_API SHALL return a 400 Bad Request response
7. WHEN no pagination parameters are provided, THE Request_Search_API SHALL default to PageNumber 1 and PageSize 20

### Requirement 8: Authorization - Regular User Access Control

**User Story:** As a system administrator, I want regular users to only see requests they own or are assigned to, so that data privacy is maintained.

#### Acceptance Criteria

1. WHEN a Regular_User makes a search request, THE Request_Search_API SHALL filter results to include only Requests where OwnerId equals the user's ID or AssignedToUserId equals the user's ID
2. THE Request_Search_API SHALL apply the authorization filter at the IQueryable level before executing the database query
3. WHEN a Regular_User attempts to access the API without valid user identification headers, THE Request_Search_API SHALL return a 401 Unauthorized response
4. THE Request_Search_API SHALL read user identity from X-User-Id header and admin status from X-Is-Admin header
5. **PRODUCTION NOTE:** In production environments, user identity and admin status MUST be extracted from a validated JWT token via ClaimsPrincipal (not headers) to prevent header forgery attacks

### Requirement 9: Authorization - Administrator Access

**User Story:** As an administrator, I want to see all requests regardless of ownership, so that I can manage the entire system.

#### Acceptance Criteria

1. WHEN an Administrator makes a search request, THE Request_Search_API SHALL return all matching Requests without applying ownership or assignment filters
2. THE Request_Search_API SHALL determine administrator status from the X-Is-Admin header value of "true" (case-insensitive)

### Requirement 10: Input Validation

**User Story:** As a developer, I want all inputs to be validated, so that the API is protected against invalid or malicious data.

#### Acceptance Criteria

1. WHEN any required parameter fails validation, THE Request_Search_API SHALL return a 400 Bad Request response with specific field-level error messages
2. THE Request_Search_API SHALL validate that enum values (Status, RequestType) are defined in their respective enums
3. THE Request_Search_API SHALL validate that date values are valid ISO 8601 format
4. THE Request_Search_API SHALL validate that numeric parameters (PageNumber, PageSize) are positive integers
5. THE Request_Search_API SHALL trim and sanitize string inputs before processing

### Requirement 11: Angular Search Form UI

**User Story:** As a user, I want a search form interface, so that I can easily enter filter criteria and view results.

#### Acceptance Criteria

1. THE Angular_Search_Form SHALL display input fields for: RequestNumber (text input), Status (multi-select dropdown), DateFrom (date picker), DateTo (date picker), RequestType (single-select dropdown)
2. THE Angular_Search_Form SHALL display sorting controls for selecting sort field and direction
3. THE Angular_Search_Form SHALL include a Search button that triggers the API call
4. THE Angular_Search_Form SHALL include a Clear/Reset button that resets all filters to default values
5. WHEN the Search button is clicked, THE Angular_Search_Form SHALL disable all inputs and display a loading indicator until results are received

### Requirement 12: Angular Results Display

**User Story:** As a user, I want search results displayed in a table, so that I can view and analyze the data.

#### Acceptance Criteria

1. THE Angular_Search_Form SHALL display results in a table with columns: RequestNumber, Status, RequestType, CustomerId, OwnerId, AssignedToUserId, CreatedAt
2. THE Angular_Search_Form SHALL display pagination controls showing current page, total pages, and navigation buttons
3. WHEN no results match the search criteria, THE Angular_Search_Form SHALL display a "No results found" message
4. WHEN an API error occurs, THE Angular_Search_Form SHALL display an error message and allow the user to retry
5. THE Angular_Search_Form SHALL format date columns in a user-friendly format (DD/MM/YYYY HH:mm)
6. THE Angular_Search_Form SHALL display Status and RequestType as human-readable labels instead of numeric values

### Requirement 13: Angular Loading and Error States

**User Story:** As a user, I want clear feedback during search operations, so that I know when the system is working or when errors occur.

#### Acceptance Criteria

1. WHILE a search request is in progress, THE Angular_Search_Form SHALL display a loading spinner or progress indicator
2. WHILE a search request is in progress, THE Angular_Search_Form SHALL disable the Search button to prevent duplicate requests
3. IF an API request fails, THEN THE Angular_Search_Form SHALL display an error message describing the failure
4. IF an API request fails, THEN THE Angular_Search_Form SHALL preserve the user's filter selections
5. WHEN an error is displayed, THE Angular_Search_Form SHALL provide a way to dismiss the error and retry

### Requirement 14: API Response Structure

**User Story:** As a frontend developer, I want a consistent API response structure, so that I can reliably parse and display results.

#### Acceptance Criteria

1. THE Request_Search_API SHALL return a Paged_Result object containing: Items (array of RequestDto), TotalCount (integer), PageNumber (integer), PageSize (integer), TotalPages (integer)
2. THE Request_Search_API SHALL return HTTP 200 with the Paged_Result for successful requests
3. THE Request_Search_API SHALL return HTTP 400 with a ValidationProblemDetails object for validation failures
4. THE Request_Search_API SHALL return HTTP 401 for unauthorized access
5. THE Request_Search_API SHALL include proper CORS headers to allow frontend access

### Requirement 15: Performance Optimization

**User Story:** As a system operator, I want the search to perform efficiently, so that users have a responsive experience even with millions of records.

#### Acceptance Criteria

1. THE Request_Search_API SHALL execute the count query and data query in a manner that minimizes database round trips
2. FOR ALL search operations, THE Request_Search_API SHALL complete within 500ms for datasets up to 1 million records (with appropriate database indexes)
3. THE Request_Search_API SHALL use AsNoTracking for read-only queries to reduce memory overhead
4. THE Request_Search_API SHALL support cancellation tokens to abort long-running queries when the client disconnects
