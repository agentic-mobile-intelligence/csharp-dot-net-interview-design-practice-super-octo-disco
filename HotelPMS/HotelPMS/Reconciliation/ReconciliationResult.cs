using HotelPMS.Domain.Models;

namespace HotelPMS.Reconciliation;

public enum ReconciliationStatus
{
    InSync,
    Updated,
    Discrepancy
}

/// <summary>
/// The output of one reconciliation run for a single property + date combination.
/// Every run is persisted to an audit log so asset managers can answer
/// "why did my NOI change between Monday and Tuesday?"
/// </summary>
public class ReconciliationResult
{
    public string PropertyId { get; init; } = string.Empty;
    public DateOnly ReportDate { get; init; }
    public ReconciliationStatus Status { get; init; }
    public DateTime RunAt { get; init; } = DateTime.UtcNow;

    /// <summary>Deltas detected in this run (new, modified, or cancelled rows).</summary>
    public List<ReconciliationDelta> Deltas { get; init; } = new();

    /// <summary>
    /// Rows where the operational data contradicts a manual scorecard correction.
    /// A human must decide which version wins — this service never silently overwrites.
    /// </summary>
    public List<ReconciliationConflict> Conflicts { get; init; } = new();

    public int NewRows => Deltas.Count(d => d.Type == DeltaType.New);
    public int ModifiedRows => Deltas.Count(d => d.Type == DeltaType.Modified);
    public int CancelledRows => Deltas.Count(d => d.Type == DeltaType.Cancelled);
}

public enum DeltaType { New, Modified, Cancelled }

public record ReconciliationDelta(
    DeltaType Type,
    PropertyRow OperationalRow,
    DomainProperty? PreviousScorecardState);

public record ReconciliationConflict(
    PropertyRow OperationalRow,
    DomainProperty ScorecardRow,
    string Reason);
