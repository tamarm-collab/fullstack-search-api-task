using FluentValidation;
using Requests.Application.Requests;
using Requests.Infrastructure.Repositories;

namespace Requests.Api.Validators;

/// <summary>
/// Validator for SearchRequestQuery using FluentValidation.
/// Validates all query parameters for the search endpoint.
/// </summary>
public sealed class SearchRequestQueryValidator : AbstractValidator<SearchRequestQuery>
{
    public SearchRequestQueryValidator()
    {
        // PageNumber must be at least 1
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        // PageSize must be between 1 and 100
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        // DateFrom must be <= DateTo when both are provided
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom.Value <= x.DateTo.Value)
            .WithMessage("DateFrom must be less than or equal to DateTo.")
            .WithName("DateRange");

        // SortBy must be one of the allowed fields when provided (Single Source of Truth from Repository)
        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"SortBy must be one of the allowed fields: {string.Join(", ", RequestRepository.AllowedSortFields)}.");

        // SortDirection must be "asc" or "desc" (case-insensitive) when provided
        RuleFor(x => x.SortDirection)
            .Must(BeValidSortDirection)
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection))
            .WithMessage("SortDirection must be 'asc' or 'desc'.");

        // Validate Statuses enum values when provided
        RuleForEach(x => x.Statuses)
            .IsInEnum()
            .When(x => x.Statuses != null && x.Statuses.Count > 0)
            .WithMessage("Invalid status value provided.");

        // Validate RequestType enum value when provided
        RuleFor(x => x.RequestType)
            .IsInEnum()
            .When(x => x.RequestType.HasValue)
            .WithMessage("Invalid request type value provided.");
    }

    /// <summary>
    /// Validates that the sort field is in the allowed whitelist (from Repository - single source of truth)
    /// </summary>
    private static bool BeValidSortField(string? sortBy)
    {
        return string.IsNullOrWhiteSpace(sortBy) || 
               RequestRepository.AllowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that the sort direction is "asc" or "desc" (case-insensitive)
    /// </summary>
    private static bool BeValidSortDirection(string? sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortDirection))
        {
            return true;
        }

        return sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
               sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
    }
}
