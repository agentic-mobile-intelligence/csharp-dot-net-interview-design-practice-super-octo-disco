using HotelPMS.Domain.Enums;

namespace HotelPMS.Domain.Models;

/// <summary>
/// Transformed, validated domain object ready for persistence and NOI calculation.
/// </summary>
public class DomainProperty
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PropertyId { get; set; } = string.Empty;
    public PropertySector Sector { get; set; }
    public DateOnly ReportDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal NOI => TotalRevenue - OperatingExpenses;

    // Sector-specific enriched fields
    public decimal? OccupancyRate { get; set; }
    public string? SectorClassification { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
