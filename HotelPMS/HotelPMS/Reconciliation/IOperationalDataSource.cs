using HotelPMS.Domain.Models;

namespace HotelPMS.Reconciliation;

/// <summary>
/// Read-only contract for pulling a snapshot from an operational PMS system
/// (Opera, Maestro, or any proprietary hotel system).
///
/// Hubtricity NEVER writes back through this interface — the PMS is an external
/// source of truth. We pull; we never push.
/// </summary>
public interface IOperationalDataSource
{
    /// <summary>
    /// Returns all operational rows for the given property and report date.
    /// Implementations call the PMS API (or SFTP file drop) without side effects.
    /// </summary>
    Task<IReadOnlyList<PropertyRow>> PullSnapshotAsync(
        string propertyId,
        DateOnly reportDate,
        CancellationToken ct = default);
}
