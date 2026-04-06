using HotelPMS.Domain.Enums;
using HotelPMS.Domain.Models;

namespace HotelPMS.Transformers;

public class MultifamilyTransformer : IDataTransformer
{
    public Task<DomainProperty> TransformAsync(PropertyRow row, CancellationToken ct = default)
    {
        var annualRent = (row.MonthlyRent ?? 0m) * 12m;

        var domain = new DomainProperty
        {
            PropertyId = row.PropertyId,
            Sector = PropertySector.Multifamily,
            ReportDate = row.ReportDate,
            TotalRevenue = annualRent + (row.OtherRevenue ?? 0m),
            OperatingExpenses = row.OperatingExpenses ?? 0m,
            SectorClassification = row.UnitNumber
        };
        return Task.FromResult(domain);
    }
}
