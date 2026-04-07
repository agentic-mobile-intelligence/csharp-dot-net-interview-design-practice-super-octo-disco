using HotelPMS.Domain.Models;

namespace HotelPMS.Validators;

/// <summary>
/// Hotels validate room types, occupancy, and revenue per available room (RevPAR).
/// </summary>
public class HotelValidationStrategy : IValidationStrategy
{
    private static readonly HashSet<string> ValidRoomTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Standard", "Deluxe", "Suite", "Penthouse" };

    public ValidationResult Validate(PropertyRow row)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(row.PropertyId))
            result.AddError("PropertyId is required.");

        if (string.IsNullOrWhiteSpace(row.RoomType))
            result.AddError("RoomType is required for hotel rows.");
        else if (!ValidRoomTypes.Contains(row.RoomType))
            result.AddWarning($"Unknown RoomType '{row.RoomType}'. Expected: {string.Join(", ", ValidRoomTypes)}.");

        if (row.TotalRooms is null or <= 0)
            result.AddError("TotalRooms must be a positive integer.");

        if (row.OccupiedRooms is null or < 0)
            result.AddError("OccupiedRooms cannot be negative.");

        if (row.OccupiedRooms > row.TotalRooms)
            result.AddError($"OccupiedRooms ({row.OccupiedRooms}) cannot exceed TotalRooms ({row.TotalRooms}).");

        if (row.RoomRevenue is null or < 0)
            result.AddError("RoomRevenue must be zero or positive.");

        if (row.ReportDate == default)
            result.AddError("ReportDate is required.");

        return result;
    }
}
