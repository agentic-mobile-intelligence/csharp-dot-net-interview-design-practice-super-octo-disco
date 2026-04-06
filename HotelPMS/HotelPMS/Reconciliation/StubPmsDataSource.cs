using HotelPMS.Domain.Enums;
using HotelPMS.Domain.Models;

namespace HotelPMS.Reconciliation;

/// <summary>
/// Stub implementation of IOperationalDataSource for demos and unit tests.
/// Replace with an implementation that calls Opera/Maestro REST APIs or reads
/// from an SFTP file drop in production.
/// </summary>
public class StubPmsDataSource : IOperationalDataSource
{
    public Task<IReadOnlyList<PropertyRow>> PullSnapshotAsync(
        string propertyId,
        DateOnly reportDate,
        CancellationToken ct = default)
    {
        // Simulates a PMS returning two reservation rows for a hotel
        IReadOnlyList<PropertyRow> rows = new List<PropertyRow>
        {
            new()
            {
                PropertyId      = propertyId,
                Sector          = PropertySector.Hotel,
                ReportDate      = reportDate,
                RoomType        = "Standard",
                TotalRooms      = 100,
                OccupiedRooms   = 78,
                RoomRevenue     = 15_600m,
                OperatingExpenses = 8_200m,
                SourceRowNumber = 1
            },
            new()
            {
                PropertyId      = propertyId,
                Sector          = PropertySector.Hotel,
                ReportDate      = reportDate,
                RoomType        = "Suite",
                TotalRooms      = 20,
                OccupiedRooms   = 14,
                RoomRevenue     = 9_800m,
                OperatingExpenses = 3_100m,
                SourceRowNumber = 2
            }
        };

        return Task.FromResult(rows);
    }
}
