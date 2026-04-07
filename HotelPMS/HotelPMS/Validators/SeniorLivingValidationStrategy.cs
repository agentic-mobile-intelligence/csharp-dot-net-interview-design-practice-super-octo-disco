using HotelPMS.Domain.Models;

namespace HotelPMS.Validators;

/// <summary>
/// Senior living validates care levels, licensed bed counts, and care revenue.
/// </summary>
public class SeniorLivingValidationStrategy : IValidationStrategy
{
    private static readonly HashSet<string> ValidCareLevels =
        new(StringComparer.OrdinalIgnoreCase) { "Independent", "Assisted", "Memory", "SkilledNursing" };

    public ValidationResult Validate(PropertyRow row)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(row.PropertyId))
            result.AddError("PropertyId is required.");

        if (string.IsNullOrWhiteSpace(row.CareLevel))
            result.AddError("CareLevel is required for senior living rows.");
        else if (!ValidCareLevels.Contains(row.CareLevel))
            result.AddError($"Invalid CareLevel '{row.CareLevel}'. Expected: {string.Join(", ", ValidCareLevels)}.");

        if (row.LicensedBeds is null or <= 0)
            result.AddError("LicensedBeds must be a positive integer.");

        if (row.OccupiedBeds is null or < 0)
            result.AddError("OccupiedBeds cannot be negative.");

        if (row.OccupiedBeds > row.LicensedBeds)
            result.AddError($"OccupiedBeds ({row.OccupiedBeds}) cannot exceed LicensedBeds ({row.LicensedBeds}).");

        if (row.CareRevenue is null or < 0)
            result.AddError("CareRevenue must be zero or positive.");

        if (row.ReportDate == default)
            result.AddError("ReportDate is required.");

        return result;
    }
}
