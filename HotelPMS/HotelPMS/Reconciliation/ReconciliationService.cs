using HotelPMS.Domain.Models;
using HotelPMS.Factories;
using HotelPMS.Repository;

namespace HotelPMS.Reconciliation;

/// <summary>
/// Pull-model reconciliation service.
///
/// Design decisions captured here:
///
/// 1. READ-ONLY pull   — we never write back to the PMS. The PMS is the operational
///    source of truth; Hubtricity's scorecard is the financial source of truth.
///    These are deliberately separate concerns.
///
/// 2. SNAPSHOT diff    — we pull the full current state from the PMS and compare it
///    against what the scorecard currently holds. For PMS systems that expose a
///    changelog API, swap PullSnapshotAsync for a PullChangelogAsync overload and
///    skip the diff — the interface stays identical.
///
/// 3. CONFLICT detection — if the operational data contradicts a manual scorecard
///    correction, we flag it for human review rather than silently overwriting.
///    Business rules (not code) decide which version wins.
///
/// 4. AUDIT TRAIL      — every run produces a ReconciliationResult that is persisted.
///    Asset managers can query it to answer "why did my NOI change on Tuesday?"
///
/// 5. CADENCE          — the caller (a scheduled job, a timer trigger, or a manual
///    API call) controls how often this runs. Nightly is usually fine for month-end
///    NOI reporting. 15-minute pulls suit same-day pricing decisions. The service
///    itself is frequency-agnostic.
/// </summary>
public class ReconciliationService
{
    private readonly IOperationalDataSource _pmsSource;
    private readonly IUnitOfWork _unitOfWork;

    public ReconciliationService(IOperationalDataSource pmsSource, IUnitOfWork unitOfWork)
    {
        _pmsSource = pmsSource;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReconciliationResult> ReconcileAsync(
        string propertyId,
        DateOnly reportDate,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[RECONCILE] Starting for property '{propertyId}' on {reportDate}");

        // ── Step 1: Pull operational snapshot (read-only) ────────────────────
        var operationalRows = await _pmsSource.PullSnapshotAsync(propertyId, reportDate, ct);
        Console.WriteLine($"[RECONCILE] Pulled {operationalRows.Count} rows from PMS");

        // ── Step 2: Load current scorecard state ─────────────────────────────
        var scorecardRows = await _unitOfWork.DomainProperties
            .GetByPropertyIdAsync(propertyId, ct);
        var scorecardByDate = scorecardRows
            .Where(r => r.ReportDate == reportDate)
            .ToDictionary(r => r.PropertyId + "|" + r.ReportDate);

        // ── Step 3: Diff operational vs scorecard ────────────────────────────
        var deltas = new List<ReconciliationDelta>();
        var conflicts = new List<ReconciliationConflict>();

        foreach (var opRow in operationalRows)
        {
            var key = opRow.PropertyId + "|" + opRow.ReportDate;

            if (!scorecardByDate.TryGetValue(key, out var scorecardRow))
            {
                // Net-new row in the PMS — not yet in the scorecard
                deltas.Add(new ReconciliationDelta(DeltaType.New, opRow, null));
            }
            else
            {
                // Row exists in both — check for material differences
                var operationalNOI = (opRow.RoomRevenue ?? opRow.CareRevenue ?? opRow.MonthlyRent * 12 ?? 0m)
                                   - (opRow.OperatingExpenses ?? 0m);

                var noiDrift = Math.Abs(scorecardRow.NOI - operationalNOI);

                if (noiDrift > 0.01m) // $0.01 tolerance for floating-point rounding
                {
                    // Check for manual override conflict:
                    // If the scorecard row was manually corrected more recently than
                    // the operational snapshot, it may be intentional. Flag for review.
                    if (WasManuallyOverridden(scorecardRow))
                    {
                        conflicts.Add(new ReconciliationConflict(
                            opRow,
                            scorecardRow,
                            $"NOI drift of ${noiDrift:N2}: operational=${operationalNOI:N2}, scorecard=${scorecardRow.NOI:N2}. Scorecard was manually overridden — human review required."));
                    }
                    else
                    {
                        deltas.Add(new ReconciliationDelta(DeltaType.Modified, opRow, scorecardRow));
                    }
                }
            }
        }

        // ── Step 4: Apply non-conflicting deltas ─────────────────────────────
        var updatedCount = 0;
        if (deltas.Count > 0)
        {
            var factory = PropertySectorFactoryResolver.Resolve(
                operationalRows.First().Sector, withDecorators: false);
            var transformer = factory.CreateTransformer();

            foreach (var delta in deltas)
            {
                var updated = await transformer.TransformAsync(delta.OperationalRow, ct);
                ((InMemoryUnitOfWork)_unitOfWork).Stage(updated);
                updatedCount++;
            }

            await _unitOfWork.CommitAsync(ct);
        }

        // ── Step 5: Build audit result ───────────────────────────────────────
        var status = conflicts.Count > 0
            ? ReconciliationStatus.Discrepancy
            : deltas.Count > 0
                ? ReconciliationStatus.Updated
                : ReconciliationStatus.InSync;

        var result = new ReconciliationResult
        {
            PropertyId = propertyId,
            ReportDate = reportDate,
            Status = status,
            Deltas = deltas,
            Conflicts = conflicts
        };

        Console.WriteLine($"[RECONCILE] Complete. Status: {status}. New: {result.NewRows}, Modified: {result.ModifiedRows}, Conflicts: {conflicts.Count}");

        if (conflicts.Count > 0)
        {
            Console.WriteLine($"[RECONCILE] ⚠ {conflicts.Count} conflict(s) require human review:");
            foreach (var conflict in conflicts)
                Console.WriteLine($"  - {conflict.Reason}");
        }

        return result;
    }

    // In production, this checks an `IsManualOverride` flag or an audit log timestamp.
    private static bool WasManuallyOverridden(DomainProperty row) => false;
}
