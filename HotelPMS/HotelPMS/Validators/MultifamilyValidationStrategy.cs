using HotelPMS.Domain.Models;

namespace HotelPMS.Validators;

/// <summary>
/// Multifamily validates unit numbers, lease term integrity, and rent amounts.
/// </summary>
public class MultifamilyValidationStrategy : IValidationStrategy
{
    public ValidationResult Validate(PropertyRow row)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(row.PropertyId))
            result.AddError("PropertyId is required.");

        if (string.IsNullOrWhiteSpace(row.UnitNumber))
            result.AddError("UnitNumber is required for multifamily rows.");

        if (row.LeaseStartDate is null)
            result.AddError("LeaseStartDate is required.");

        if (row.LeaseEndDate is null)
            result.AddError("LeaseEndDate is required.");

        if (row.LeaseStartDate.HasValue && row.LeaseEndDate.HasValue
            && row.LeaseEndDate <= row.LeaseStartDate)
            result.AddError("LeaseEndDate must be after LeaseStartDate.");

        if (row.MonthlyRent is null or < 0)
            result.AddError("MonthlyRent must be zero or positive.");

        if (row.MonthlyRent > 50_000)
            result.AddWarning($"MonthlyRent ${row.MonthlyRent:N2} is unusually high — verify.");

        if (row.ReportDate == default)
            result.AddError("ReportDate is required.");

        return result;
    }
}
