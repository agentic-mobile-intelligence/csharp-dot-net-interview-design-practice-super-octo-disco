using HotelPMS.Domain.Enums;
using HotelPMS.Domain.Models;

namespace HotelPMS.Repository;

/// <summary>
/// In-memory implementation — used for demos, unit tests, and local development
/// without a real database connection. Swap for a SqlServerDomainPropertyRepository
/// in production by changing the DI registration only.
/// </summary>
public class InMemoryDomainPropertyRepository : IDomainPropertyRepository
{
    private readonly Dictionary<Guid, DomainProperty> _store = new();

    public Task<DomainProperty?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task<IReadOnlyList<DomainProperty>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DomainProperty>>(_store.Values.ToList());

    public Task AddAsync(DomainProperty entity, CancellationToken ct = default)
    {
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<DomainProperty> entities, CancellationToken ct = default)
    {
        foreach (var entity in entities)
            _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DomainProperty entity, CancellationToken ct = default)
    {
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _store.Remove(id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DomainProperty>> GetBySectorAsync(PropertySector sector, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DomainProperty>>(
            _store.Values.Where(p => p.Sector == sector).ToList());

    public Task<IReadOnlyList<DomainProperty>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DomainProperty>>(
            _store.Values.Where(p => p.ReportDate >= from && p.ReportDate <= to).ToList());

    public Task<IReadOnlyList<DomainProperty>> GetByPropertyIdAsync(string propertyId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DomainProperty>>(
            _store.Values.Where(p => p.PropertyId == propertyId).ToList());
}
