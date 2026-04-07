using HotelPMS.Domain.Enums;

namespace HotelPMS.Domain.Models;

/// <summary>
/// Raw row ingested from an Excel PMS file before transformation.
/// </summary>
public class PropertyRow
{
    public string PropertyId { get; set; } = string.Empty;
    public PropertySector Sector { get; set; }
    public DateOnly ReportDate { get; set; }

    // Hotel-specific
    public string? RoomType { get; set; }
    public int? OccupiedRooms { get; set; }
    public int? TotalRooms { get; set; }
    public decimal? RoomRevenue { get; set; }

    // Multifamily-specific
    public string? UnitNumber { get; set; }
    public DateOnly? LeaseStartDate { get; set; }
    public DateOnly? LeaseEndDate { get; set; }
    public decimal? MonthlyRent { get; set; }

    // Senior living-specific
    public string? CareLevel { get; set; }        // e.g., "Independent", "Assisted", "Memory"
    public decimal? CareRevenue { get; set; }
    public int? LicensedBeds { get; set; }
    public int? OccupiedBeds { get; set; }

    // Common financials
    public decimal? OperatingExpenses { get; set; }
    public decimal? OtherRevenue { get; set; }

    // Raw Excel metadata
    public int SourceRowNumber { get; set; }
    public Dictionary<string, string> RawFields { get; set; } = new();
}
