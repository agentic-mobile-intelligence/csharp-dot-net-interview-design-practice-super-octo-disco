using HotelPMS.Domain.Enums;
using HotelPMS.Domain.Models;

namespace HotelPMS.Transformers;

public class SeniorLivingTransformer : IDataTransformer
{
    public Task<DomainProperty> TransformAsync(PropertyRow row, CancellationToken ct = default)
    {
        var domain = new DomainProperty
        {
            PropertyId = row.PropertyId,
            Sector = PropertySector.SeniorLiving,
            ReportDate = row.ReportDate,
            TotalRevenue = (row.CareRevenue ?? 0m) + (row.OtherRevenue ?? 0m),
            OperatingExpenses = row.OperatingExpenses ?? 0m,
            SectorClassification = row.CareLevel,
            OccupancyRate = row.LicensedBeds > 0
                ? (decimal)(row.OccupiedBeds ?? 0) / row.LicensedBeds.Value
                : null
        };
        return Task.FromResult(domain);
    }
}
