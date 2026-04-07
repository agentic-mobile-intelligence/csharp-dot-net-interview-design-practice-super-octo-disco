using HotelPMS.Domain.Models;

namespace HotelPMS.Repository;

/// <summary>
/// In-memory Unit of Work. Tracks pending writes in a staging list;
/// CommitAsync flushes them to the underlying repository atomically.
///
/// In a real EF Core implementation, CommitAsync calls DbContext.SaveChangesAsync()
/// inside a database transaction. The interface stays the same — the caller never knows.
/// </summary>
public class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly InMemoryDomainPropertyRepository _repository;
    private readonly List<DomainProperty> _pendingAdds = new();
    private bool _disposed;

    public InMemoryUnitOfWork()
    {
        _repository = new InMemoryDomainPropertyRepository();
        DomainProperties = _repository;
    }

    public IDomainPropertyRepository DomainProperties { get; }

    /// <summary>Stages an entity to be written on the next CommitAsync.</summary>
    public void Stage(DomainProperty entity) => _pendingAdds.Add(entity);

    /// <summary>Stages multiple entities to be bulk-written on the next CommitAsync.</summary>
    public void StageRange(IEnumerable<DomainProperty> entities) => _pendingAdds.AddRange(entities);

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        if (_pendingAdds.Count == 0)
            return 0;

        // In production: BEGIN TRANSACTION → bulk insert → COMMIT
        await _repository.AddRangeAsync(_pendingAdds, ct);
        var count = _pendingAdds.Count;
        _pendingAdds.Clear();

        Console.WriteLine($"[UoW] Committed {count} entities.");
        return count;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        var count = _pendingAdds.Count;
        _pendingAdds.Clear();
        Console.WriteLine($"[UoW] Rolled back {count} staged entities.");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _pendingAdds.Clear();
        _disposed = true;
    }
}
