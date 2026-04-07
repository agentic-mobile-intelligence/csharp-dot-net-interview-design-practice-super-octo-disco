using HotelPMS.Domain.Enums;
using HotelPMS.Domain.Models;

namespace HotelPMS.Repository;

/// <summary>
/// Sector- and date-aware queries on top of the generic repository contract.
/// </summary>
public interface IDomainPropertyRepository : IRepository<DomainProperty>
{
    Task<IReadOnlyList<DomainProperty>> GetBySectorAsync(PropertySector sector, CancellationToken ct = default);
    Task<IReadOnlyList<DomainProperty>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<DomainProperty>> GetByPropertyIdAsync(string propertyId, CancellationToken ct = default);
}
