using HotelPMS.Domain.Models;

namespace HotelPMS.Validators;

/// <summary>
/// Context class that delegates validation to the injected strategy.
/// Swap the strategy at construction time to change sector behavior.
/// </summary>
public class PropertyRowValidator
{
    private readonly IValidationStrategy _strategy;

    public PropertyRowValidator(IValidationStrategy strategy) => _strategy = strategy;

    public ValidationResult Validate(PropertyRow row) => _strategy.Validate(row);

    public IEnumerable<ValidationResult> ValidateAll(IEnumerable<PropertyRow> rows) =>
        rows.Select(row => _strategy.Validate(row));
}
