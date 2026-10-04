using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Requests.Application.Requests;

namespace Requests.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _service;
    private readonly IValidator<SearchRequestQuery> _validator;

    public RequestsController(IRequestService service, IValidator<SearchRequestQuery> validator)
    {
        _service = service;
        _validator = validator;
    }

    /// <summary>
    /// Search and filter requests with pagination
    /// </summary>
    /// <remarks>
    /// SECURITY NOTE: This implementation uses X-User-Id and X-Is-Admin headers for demo/testing purposes.
    /// In a production environment, user identity and admin status MUST be extracted from a validated
    /// JWT token via ClaimsPrincipal (User.FindFirst) to prevent header forgery attacks.
    /// </remarks>
    /// <param name="query">Search query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result containing matching requests</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<RequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<RequestDto>>> Search(
        [FromQuery] SearchRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        }

        // Extract user from headers (in real implementation would use claims from JWT)
        var currentUserId = ParseUserId(Request.Headers["X-User-Id"].FirstOrDefault());
        if (!currentUserId.HasValue)
        {
            return Unauthorized("X-User-Id header is required");
        }
        
        var isAdministrator = string.Equals(
            Request.Headers["X-Is-Admin"].FirstOrDefault(),
            "true",
            StringComparison.OrdinalIgnoreCase);

        var result = await _service.SearchAsync(query, currentUserId.Value, isAdministrator, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Parse user ID from header string. Returns null if invalid or missing.
    /// </summary>
    private static int? ParseUserId(string? value)
        => int.TryParse(value, out var userId) && userId > 0 ? userId : null;
}
