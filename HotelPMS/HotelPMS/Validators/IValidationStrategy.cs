using HotelPMS.Domain.Models;

namespace HotelPMS.Validators;

/// <summary>
/// Strategy pattern: each sector implements its own validation rules.
/// </summary>
public interface IValidationStrategy
{
    ValidationResult Validate(PropertyRow row);
}
