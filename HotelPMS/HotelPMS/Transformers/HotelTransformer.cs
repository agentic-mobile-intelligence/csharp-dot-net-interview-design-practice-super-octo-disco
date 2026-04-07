using HotelPMS.Domain.Enums;
using HotelPMS.Domain.Models;

namespace HotelPMS.Transformers;

public class HotelTransformer : IDataTransformer
{
    public Task<DomainProperty> TransformAsync(PropertyRow row, CancellationToken ct = default)
    {
        var domain = new DomainProperty
        {
            PropertyId = row.PropertyId,
            Sector = PropertySector.Hotel,
            ReportDate = row.ReportDate,
            TotalRevenue = (row.RoomRevenue ?? 0m) + (row.OtherRevenue ?? 0m),
            OperatingExpenses = row.OperatingExpenses ?? 0m,
            SectorClassification = row.RoomType,
            OccupancyRate = row.TotalRooms > 0
                ? (decimal)(row.OccupiedRooms ?? 0) / row.TotalRooms.Value
                : null
        };
        return Task.FromResult(domain);
    }
}
