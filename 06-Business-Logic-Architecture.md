# Business Logic Architecture: Domain Layer vs API vs Database

Where you place business logic determines how testable, maintainable, and portable your application is. This guide covers the layered approach with a real-world hospitality/property management system (PMS) domain.

---

## The Three Options

| Layer | What Lives Here | Example |
|-------|----------------|---------|
| **Database** | Stored procedures, triggers, constraints | CHECK constraint on room price > 0 |
| **API / Controller** | Request routing, input validation, HTTP concerns | Validate JSON payload, return 400 on bad input |
| **Domain / Service** | Core business rules, calculations, transformations | Calculate total stay cost with taxes and discounts |

---

## The Answer: Domain Layer

Business logic belongs in the **domain layer**. The API layer orchestrates requests and the database layer persists data, but neither should own the rules.

### Why It Matters

- **Testability** - Domain logic can be unit tested without HTTP context or database connections
- **Portability** - Rules work whether data comes from a REST API, CSV import, Excel upload, or message queue
- **Single source of truth** - One place to change a business rule, not scattered across controllers and stored procedures
- **Team scalability** - Developers can work on domain logic independently of API or database concerns

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│                    API Layer                          │
│  Controllers / Endpoints                             │
│  - HTTP routing, request/response mapping            │
│  - Input validation (format, required fields)        │
│  - Authentication / Authorization                    │
│  - Calls into domain services via interface contracts │
└──────────────────┬───────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────┐
│                 Domain Layer                          │
│  Services, Models, Interfaces                        │
│  - Business rules and calculations                   │
│  - Data transformation and aggregation               │
│  - Domain models shared across boundaries            │
│  - NO dependency on HTTP, database, or file formats  │
└──────────────────┬───────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────┐
│              Infrastructure Layer                     │
│  Repositories, File Readers, External APIs           │
│  - Database access (EF Core, Dapper)                 │
│  - CSV/Excel/PMS file parsing                        │
│  - Third-party service integrations                  │
└──────────────────────────────────────────────────────┘
```

---

## Real-World Example: Hospitality PMS

A property management system ingests data from multiple sources — PMS exports, Excel spreadsheets, CSV files — and exposes it through an API. The domain layer is where all the business rules live.

### Shared Domain Models

Domain models represent the core concepts. These are shared across layers as the common language.

```csharp
// Domain/Models/Room.cs
namespace Domain.Models;

public class Room
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; }
    public RoomType Type { get; set; }
    public decimal NightlyRate { get; set; }
    public bool IsAvailable { get; set; }
}

public enum RoomType
{
    Standard,
    Deluxe,
    Suite,
    Penthouse
}
```

```csharp
// Domain/Models/Reservation.cs
namespace Domain.Models;

public class Reservation
{
    public int ReservationId { get; set; }
    public int RoomId { get; set; }
    public string GuestName { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal TotalCost { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Captured,
    Refunded,
    Failed
}
```

```csharp
// Domain/Models/PaymentTransaction.cs
namespace Domain.Models;

public class PaymentTransaction
{
    public int TransactionId { get; set; }
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime TransactionDate { get; set; }
    public string ReferenceNumber { get; set; }
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    BankTransfer,
    Cash
}
```

### Domain Interfaces (Contracts)

The domain layer defines what it needs through interfaces. The API and infrastructure layers implement these contracts.

```csharp
// Domain/Interfaces/IRoomRepository.cs
namespace Domain.Interfaces;

public interface IRoomRepository
{
    Room GetById(int roomId);
    IEnumerable<Room> GetAvailableRooms(DateTime checkIn, DateTime checkOut);
    void Update(Room room);
}
```

```csharp
// Domain/Interfaces/IReservationRepository.cs
namespace Domain.Interfaces;

public interface IReservationRepository
{
    Reservation GetById(int reservationId);
    IEnumerable<Reservation> GetByDateRange(DateTime start, DateTime end);
    void Add(Reservation reservation);
    void Update(Reservation reservation);
}
```

```csharp
// Domain/Interfaces/IPmsDataSource.cs
namespace Domain.Interfaces;

// Abstraction for external data sources (CSV, Excel, PMS exports)
public interface IPmsDataSource
{
    IEnumerable<Room> ImportRooms();
    IEnumerable<Reservation> ImportReservations();
    IEnumerable<PaymentTransaction> ImportTransactions();
}
```

### Domain Services (Business Logic Lives Here)

```csharp
// Domain/Services/ReservationService.cs
namespace Domain.Services;

public class ReservationService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IReservationRepository _reservationRepository;

    public ReservationService(
        IRoomRepository roomRepository,
        IReservationRepository reservationRepository)
    {
        _roomRepository = roomRepository;
        _reservationRepository = reservationRepository;
    }

    // Business rule: calculate total stay cost
    public decimal CalculateStayCost(Room room, DateTime checkIn, DateTime checkOut)
    {
        int nights = (checkOut - checkIn).Days;
        if (nights <= 0)
            throw new DomainException("Check-out must be after check-in.");

        decimal baseCost = room.NightlyRate * nights;

        // Business rule: 7+ night stays get 10% discount
        if (nights >= 7)
            baseCost *= 0.90m;

        // Business rule: suites and penthouses include a 12% service charge
        if (room.Type == RoomType.Suite || room.Type == RoomType.Penthouse)
            baseCost *= 1.12m;

        return Math.Round(baseCost, 2);
    }

    // Business rule: create a reservation with validation
    public Reservation CreateReservation(int roomId, string guestName,
        DateTime checkIn, DateTime checkOut)
    {
        var room = _roomRepository.GetById(roomId);
        if (room == null)
            throw new DomainException($"Room {roomId} not found.");

        if (!room.IsAvailable)
            throw new DomainException($"Room {room.RoomNumber} is not available.");

        var totalCost = CalculateStayCost(room, checkIn, checkOut);

        var reservation = new Reservation
        {
            RoomId = roomId,
            GuestName = guestName,
            CheckIn = checkIn,
            CheckOut = checkOut,
            TotalCost = totalCost,
            PaymentStatus = PaymentStatus.Pending
        };

        _reservationRepository.Add(reservation);

        // Mark room as unavailable
        room.IsAvailable = false;
        _roomRepository.Update(room);

        return reservation;
    }
}
```

```csharp
// Domain/Services/PmsImportService.cs
namespace Domain.Services;

// Handles data coming from different external systems
public class PmsImportService
{
    private readonly IPmsDataSource _dataSource;
    private readonly IRoomRepository _roomRepository;
    private readonly IReservationRepository _reservationRepository;

    public PmsImportService(
        IPmsDataSource dataSource,
        IRoomRepository roomRepository,
        IReservationRepository reservationRepository)
    {
        _dataSource = dataSource;
        _roomRepository = roomRepository;
        _reservationRepository = reservationRepository;
    }

    // Business logic for reconciling imported data with existing records
    public ImportResult ImportFromExternalSource()
    {
        var result = new ImportResult();

        var importedRooms = _dataSource.ImportRooms();
        foreach (var room in importedRooms)
        {
            var existing = _roomRepository.GetById(room.RoomId);
            if (existing != null)
            {
                // Business rule: only update rate if the imported rate is valid
                if (room.NightlyRate > 0)
                {
                    existing.NightlyRate = room.NightlyRate;
                    existing.Type = room.Type;
                    _roomRepository.Update(existing);
                    result.UpdatedRooms++;
                }
                else
                {
                    result.SkippedRooms++;
                }
            }
            else
            {
                result.NewRooms++;
            }
        }

        return result;
    }
}

public class ImportResult
{
    public int NewRooms { get; set; }
    public int UpdatedRooms { get; set; }
    public int SkippedRooms { get; set; }
}
```

```csharp
// Domain/Exceptions/DomainException.cs
namespace Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

### API Layer (Orchestration Only)

The API layer maps HTTP requests to domain calls. It does **not** contain business rules.

```csharp
// Api/Controllers/ReservationsController.cs
[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService _reservationService;

    public ReservationsController(ReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateReservationRequest request)
    {
        // API concern: input format validation
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest("Guest name is required.");

        try
        {
            // Delegate to domain layer for business logic
            var reservation = _reservationService.CreateReservation(
                request.RoomId,
                request.GuestName,
                request.CheckIn,
                request.CheckOut);

            return CreatedAtAction(nameof(GetById),
                new { id = reservation.ReservationId }, reservation);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id) { /* ... */ }
}
```

```csharp
// Api/Contracts/CreateReservationRequest.cs
// API-level contract — similar shape to domain model but specific to HTTP
public class CreateReservationRequest
{
    public int RoomId { get; set; }
    public string GuestName { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
}
```

### Infrastructure Layer (Data Access)

The infrastructure layer implements the domain interfaces. It knows about databases, file formats, and external systems.

```csharp
// Infrastructure/Repositories/RoomRepository.cs
namespace Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly HotelDbContext _context;

    public RoomRepository(HotelDbContext context)
    {
        _context = context;
    }

    public Room GetById(int roomId) =>
        _context.Rooms.FirstOrDefault(r => r.RoomId == roomId);

    public IEnumerable<Room> GetAvailableRooms(DateTime checkIn, DateTime checkOut) =>
        _context.Rooms.Where(r => r.IsAvailable).ToList();

    public void Update(Room room)
    {
        _context.Rooms.Update(room);
        _context.SaveChanges();
    }
}
```

```csharp
// Infrastructure/DataSources/CsvPmsDataSource.cs
namespace Infrastructure.DataSources;

// One implementation of IPmsDataSource for CSV files
public class CsvPmsDataSource : IPmsDataSource
{
    private readonly string _filePath;

    public CsvPmsDataSource(string filePath)
    {
        _filePath = filePath;
    }

    public IEnumerable<Room> ImportRooms()
    {
        return File.ReadAllLines(_filePath)
            .Skip(1) // header row
            .Select(line =>
            {
                var fields = line.Split(',');
                return new Room
                {
                    RoomId = int.Parse(fields[0]),
                    RoomNumber = fields[1],
                    Type = Enum.Parse<RoomType>(fields[2]),
                    NightlyRate = decimal.Parse(fields[3]),
                    IsAvailable = bool.Parse(fields[4])
                };
            });
    }

    public IEnumerable<Reservation> ImportReservations() { /* ... */ }
    public IEnumerable<PaymentTransaction> ImportTransactions() { /* ... */ }
}
```

```csharp
// Infrastructure/DataSources/ExcelPmsDataSource.cs
namespace Infrastructure.DataSources;

// Another implementation for Excel files — same interface, different source
public class ExcelPmsDataSource : IPmsDataSource
{
    private readonly string _filePath;

    public ExcelPmsDataSource(string filePath)
    {
        _filePath = filePath;
    }

    public IEnumerable<Room> ImportRooms()
    {
        // Use a library like EPPlus or ClosedXML to parse Excel
        // Returns the same domain models regardless of file format
        throw new NotImplementedException();
    }

    public IEnumerable<Reservation> ImportReservations() { /* ... */ }
    public IEnumerable<PaymentTransaction> ImportTransactions() { /* ... */ }
}
```

### Shared Types Between Layers

The domain models (`Room`, `Reservation`, `PaymentTransaction`) are the shared contract. Both the database layer and the API layer reference these types.

```csharp
// Infrastructure/Data/HotelDbContext.cs
namespace Infrastructure.Data;

// Database models map directly to domain models via shared types
public class HotelDbContext : DbContext
{
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<PaymentTransaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Database concerns: column types, constraints, indexes
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.RoomId);
            entity.Property(r => r.NightlyRate).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.ReservationId);
            entity.Property(r => r.TotalCost).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(t => t.TransactionId);
            entity.Property(t => t.Amount).HasColumnType("decimal(10,2)");
        });
    }
}
```

### DI Registration (Wiring It All Together)

```csharp
// Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Infrastructure
builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

// Data sources — swap CSV for Excel by changing this one line
builder.Services.AddScoped<IPmsDataSource>(sp =>
    new CsvPmsDataSource("data/pms-export.csv"));

// Domain services
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<PmsImportService>();
```

---

## What Goes Where: Quick Reference

| Concern | Layer | Example |
|---------|-------|---------|
| HTTP status codes, routing | API | Return 404 when reservation not found |
| Request/response format | API | Deserialize JSON, validate required fields |
| Authentication, authorization | API | Check JWT token, verify user role |
| Room cost calculation | Domain | Nightly rate x nights, discounts, surcharges |
| Availability rules | Domain | Can't book an occupied room |
| Data reconciliation | Domain | Merge PMS import with existing records |
| Payment validation | Domain | Amount must match reservation total |
| SQL queries, EF mappings | Infrastructure | `DbContext`, repository implementations |
| File parsing (CSV, Excel) | Infrastructure | Read and map external file formats |
| Column types, indexes | Infrastructure | `decimal(10,2)`, unique constraints |

---

## Anti-Patterns to Avoid

### Business Logic in Controllers

```csharp
// BAD - cost calculation in the controller
[HttpPost]
public IActionResult Create([FromBody] CreateReservationRequest request)
{
    var room = _context.Rooms.Find(request.RoomId);
    int nights = (request.CheckOut - request.CheckIn).Days;
    decimal cost = room.NightlyRate * nights;
    if (nights >= 7) cost *= 0.90m; // Business rule leaked into API layer
    // ...
}
```

### Business Logic in Stored Procedures

```sql
-- BAD - business rules buried in the database
CREATE PROCEDURE CreateReservation
    @RoomId INT, @GuestName NVARCHAR(100),
    @CheckIn DATE, @CheckOut DATE
AS
BEGIN
    DECLARE @Nights INT = DATEDIFF(DAY, @CheckIn, @CheckOut)
    DECLARE @Rate DECIMAL(10,2)
    SELECT @Rate = NightlyRate FROM Rooms WHERE RoomId = @RoomId

    -- Business rule buried in SQL
    IF @Nights >= 7
        SET @Rate = @Rate * 0.90

    INSERT INTO Reservations (RoomId, GuestName, CheckIn, CheckOut, TotalCost)
    VALUES (@RoomId, @GuestName, @CheckIn, @CheckOut, @Rate * @Nights)
END
```

### Why These Are Problems

- **Can't unit test** controller logic without spinning up an HTTP pipeline
- **Can't unit test** stored procedures without a live database
- **Can't reuse** the calculation when importing from CSV or Excel
- **Rules get duplicated** across controllers, stored procedures, and import scripts
- **Changes require touching multiple layers** instead of one

---

## Key Takeaways

1. **Domain layer owns business logic** - calculations, rules, validations that are about *what the business does*
2. **API layer orchestrates** - it knows how to receive HTTP requests, call domain services, and return HTTP responses
3. **API contracts mirror domain models** - the request/response types are similar in shape but serve a different purpose (serialization vs business rules)
4. **Infrastructure implements domain interfaces** - the domain says *what* it needs, infrastructure says *how*
5. **Shared models are the glue** - `Room`, `Reservation`, `PaymentTransaction` are used by all layers, keeping types consistent
6. **Data source doesn't matter to the domain** - whether data comes from CSV, Excel, PMS API, or a database, the domain logic is the same
