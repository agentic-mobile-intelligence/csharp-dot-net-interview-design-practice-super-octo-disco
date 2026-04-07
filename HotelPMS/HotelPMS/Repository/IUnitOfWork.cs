namespace HotelPMS.Repository;

/// <summary>
/// Unit of Work: groups multiple repository operations into a single atomic transaction.
/// All changes are held in memory until CommitAsync is called. If anything fails,
/// RollbackAsync discards every change in the batch — no partial writes reach the database.
///
/// Pattern benefit: bulk-loading 500 NOI rows is one transaction, not 500 individual inserts.
/// A single bad row fails the whole batch cleanly with no orphaned data.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IDomainPropertyRepository DomainProperties { get; }

    Task<int> CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
