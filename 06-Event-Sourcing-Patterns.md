# Event Sourcing Patterns in C#

Understanding the distinction between state stores and event logs — and when to use each.

---

## Core Distinction

### State Store
The current snapshot. What is true **right now**. An object, a record, a materialized view.

> "Room 204 is occupied, rate $189, checked in Tuesday."

### Event Log
The history of **what happened** to get there. Each entry is immutable, ordered, and append-only.

> "Room 204 was reserved, then modified, then checked in, then upgraded."

---

## The Key Insight

The event log is the **source of truth**. The state store is just a **projection** of that log at a point in time. You can always rebuild the state store by replaying the events.

```
Event Log (immutable)          State Store (derived)
├── ReservationCreated    →    
├── RateModified          →    Current Reservation Record
├── GuestCheckedIn        →    
└── RoomUpgraded          →    
```

---

## C# Implementation

```csharp
// State store — mutable object
public class NOIScorecard
{
    public decimal CurrentNOI { get; set; }
    public DateTime LastUpdated { get; set; }
}

// Event log — immutable record
public record NOIRecalculated(
    Guid PropertyId,
    decimal PreviousNOI,
    decimal NewNOI,
    string Reason,
    DateTimeOffset OccurredAt
);
```

### Event Handler Pattern

```csharp
// Base event type
public abstract record DomainEvent(Guid Id, DateTimeOffset OccurredAt);

// Specific events
public record ReservationCreated(
    Guid ReservationId,
    Guid RoomId,
    decimal Rate,
    DateTimeOffset OccurredAt
) : DomainEvent(ReservationId, OccurredAt);

public record RateModified(
    Guid ReservationId,
    decimal PreviousRate,
    decimal NewRate,
    string Reason,
    DateTimeOffset OccurredAt
) : DomainEvent(ReservationId, OccurredAt);

public record GuestCheckedIn(
    Guid ReservationId,
    DateTimeOffset CheckInTime,
    DateTimeOffset OccurredAt
) : DomainEvent(ReservationId, OccurredAt);

// Aggregate rebuilt from events
public class Reservation
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public decimal CurrentRate { get; private set; }
    public bool IsCheckedIn { get; private set; }

    // Rebuild state by replaying the event log
    public static Reservation Replay(IEnumerable<DomainEvent> events)
    {
        var reservation = new Reservation();
        foreach (var evt in events)
        {
            reservation.Apply(evt);
        }
        return reservation;
    }

    private void Apply(DomainEvent evt)
    {
        switch (evt)
        {
            case ReservationCreated e:
                Id = e.ReservationId;
                RoomId = e.RoomId;
                CurrentRate = e.Rate;
                break;
            case RateModified e:
                CurrentRate = e.NewRate;
                break;
            case GuestCheckedIn:
                IsCheckedIn = true;
                break;
        }
    }
}
```

### Event Store Interface

```csharp
public interface IEventStore
{
    Task AppendAsync(Guid streamId, IEnumerable<DomainEvent> events);
    Task<IEnumerable<DomainEvent>> LoadAsync(Guid streamId);
}

public class ReservationService
{
    private readonly IEventStore _eventStore;
    private readonly IReservationReadModel _readModel; // State store

    public ReservationService(IEventStore eventStore, IReservationReadModel readModel)
    {
        _eventStore = eventStore;
        _readModel = readModel;
    }

    public async Task ModifyRateAsync(Guid reservationId, decimal newRate, string reason)
    {
        // Load current state from event log
        var events = await _eventStore.LoadAsync(reservationId);
        var reservation = Reservation.Replay(events);

        // Create new event
        var evt = new RateModified(
            reservationId,
            reservation.CurrentRate,
            newRate,
            reason,
            DateTimeOffset.UtcNow
        );

        // Append to log — this is the write
        await _eventStore.AppendAsync(reservationId, new[] { evt });

        // Update read model (state store) — this is optional / async
        await _readModel.UpdateRateAsync(reservationId, newRate);
    }
}
```

---

## When Each Wins

| Scenario | Use | Why |
|----------|-----|-----|
| Fast reads of current data | **State Store** | Optimized for lookups |
| History doesn't matter | **State Store** | Simpler, less overhead |
| Simple CRUD | **State Store** | No audit trail needed |
| Audit trail is non-negotiable | **Event Log** | Immutable history |
| "What changed, when, and why?" | **Event Log** | Full traceability |
| Rebuild state after a bug | **Event Log** | Replay from known-good point |
| Financial corrections / reconciliation | **Event Log** | Compliance requirement |

---

## CQRS — The Natural Companion

Event sourcing pairs naturally with **Command Query Responsibility Segregation (CQRS)**:

- **Commands** → write to the event log
- **Queries** → read from the projected state store

```csharp
// Command side — appends an event
public class ModifyRateCommand
{
    public Guid ReservationId { get; init; }
    public decimal NewRate { get; init; }
    public string Reason { get; init; }
}

// Query side — reads projected state
public class GetReservationQuery
{
    public Guid ReservationId { get; init; }
}

// Read model (state store) — optimized for the UI
public class ReservationSummary
{
    public Guid Id { get; set; }
    public string RoomNumber { get; set; }
    public decimal CurrentRate { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

---

## F# / Functional Connection

This pattern maps cleanly to functional programming because:

- Events are **immutable data** — no mutation, just new facts
- State is derived via **pure functions** (fold/reduce over the event list)
- Each event handler is a **stateless transformation**: `(State, Event) → State`

In C# this shows up as `Aggregate.Replay(events)` — effectively a `fold` operation.

---

## Interview Answer

> "A state store gives you fast access to current truth — it's optimized for reads. An event log gives you the history of how you got there — it's optimized for auditability and correctness. In a financial system, I'd use both: events as the source of truth for anything that touches critical calculations, and a projected state store for dashboard reads. The state store is disposable — if it gets corrupted you rebuild it from the log."

**The one phrase to remember:**

> **"State is a snapshot. Events are the movie."**

The snapshot tells you where you are. The movie tells you how you got there — and lets you rewind.

---

## Advantages

- **Auditability** — complete history of every change
- **Debuggability** — replay events to reproduce any past state
- **Temporal queries** — "what did this look like last Tuesday?"
- **Event-driven integration** — other services can subscribe to domain events
- **Bug recovery** — fix the projection logic and rebuild state from the same log

## Disadvantages

- **Query complexity** — reading current state requires replaying or maintaining projections
- **Storage growth** — event log grows indefinitely (mitigated with snapshots)
- **Eventual consistency** — state store may lag behind the event log
- **Learning curve** — different mental model from standard CRUD

---

## Pattern Selection

| Need | Pattern |
|------|---------|
| Current state, fast reads | State store (projected read model) |
| Immutable history, audit trail | Event log |
| Both | Event sourcing + CQRS |
| Functional transformation of state | Aggregate replay (fold over events) |
