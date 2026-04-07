# Hotel PMS ETL Pipeline: NuGet Packages & Architecture

## 🏨 Overview

A Property Management System (PMS) for hotels requires specialized data processing:
- **Daily Excel exports** from reservation systems
- **Bulk inserts/updates** to data warehouses
- **Data validation** (room rates, dates, occupancy)
- **Error tracking** at row level
- **Scheduled processing** (nightly ETL runs)
- **Structured logging** for audit trails

This guide covers the essential NuGet packages and shows how to build a production-ready hotel data pipeline.

---

## 📦 NuGet Packages by Category

### 1. Excel Ingestion (Read/Write)

#### **EPPlus** (Best-in-Class)
```csharp
// Install: dotnet add package EPPlus
// Purpose: Read/write large .xlsx files efficiently

using OfficeOpenXml;

public class ExcelReader
{
    public async Task<List<HotelRoom>> ReadRoomsFromExcelAsync(string filePath)
    {
        // EPPlus handles large files well
        var fileInfo = new FileInfo(filePath);
        using var package = new ExcelPackage(fileInfo);
        
        var worksheet = package.Workbook.Worksheets[0];
        var rooms = new List<HotelRoom>();
        
        // Start from row 2 (skip header)
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var room = new HotelRoom
            {
                RoomNumber = worksheet.Cells[row, 1].Value?.ToString() ?? "",
                RoomType = worksheet.Cells[row, 2].Value?.ToString() ?? "",
                BasePrice = decimal.Parse(worksheet.Cells[row, 3].Value?.ToString() ?? "0"),
                IsActive = bool.Parse(worksheet.Cells[row, 4].Value?.ToString() ?? "true")
            };
            rooms.Add(room);
        }
        
        return rooms;
    }
}
```

**Pros:** Handles large files, good performance, .xlsx support  
**Cons:** Paid license for commercial use (free for non-commercial)  
**Best For:** Large hotel chains with thousands of rooms

#### **ClosedXML** (Cleaner API)
```csharp
// Install: dotnet add package ClosedXML
// Purpose: More intuitive API than EPPlus

using ClosedXML.Excel;

public class ClosedXmlReader
{
    public List<HotelRoom> ReadRoomsWithClosedXml(string filePath)
    {
        var rooms = new List<HotelRoom>();
        
        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheet(1);
            
            // More fluent API
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header
            
            foreach (var row in rows)
            {
                var room = new HotelRoom
                {
                    RoomNumber = row.Cell(1).Value.ToString(),
                    RoomType = row.Cell(2).Value.ToString(),
                    BasePrice = (decimal)row.Cell(3).Value,
                    IsActive = (bool)row.Cell(4).Value
                };
                rooms.Add(room);
            }
        }
        
        return rooms;
    }
}
```

**Pros:** Cleaner, more intuitive API  
**Cons:** Slightly slower than EPPlus  
**Best For:** Developers who prefer LINQ-style querying

#### **ExcelDataReader** (Lightweight)
```csharp
// Install: dotnet add package ExcelDataReader
// Purpose: Fast, read-only, pure ingestion

using ExcelDataReader;

public class FastExcelReader
{
    public async Task<DataSet> ReadExcelFastAsync(string filePath)
    {
        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet();
                return result;
            }
        }
    }
}
```

**Pros:** Fastest, lightweight, minimal dependencies  
**Cons:** Read-only, returns DataSet  
**Best For:** High-volume daily imports where speed matters

---

### 2. Entity Framework & Bulk Operations

#### **Microsoft.EntityFrameworkCore**
```csharp
// Core ORM for data access
public class HotelDbContext : DbContext
{
    public DbSet<HotelReservation> Reservations { get; set; }
    public DbSet<HotelRoom> Rooms { get; set; }
    public DbSet<GuestProfile> Guests { get; set; }
}
```

#### **EFCore.BulkExtensions** (CRITICAL FOR HOTELS)
```csharp
// Install: dotnet add package EFCore.BulkExtensions
// Purpose: Bulk insert/update/delete without row-by-row overhead

public class BulkImportService
{
    private readonly HotelDbContext _context;
    
    // ❌ BAD: Slow row-by-row insert (10,000 rooms = slow!)
    public async Task ImportRoomsSlowAsync(List<HotelRoom> rooms)
    {
        _context.Rooms.AddRange(rooms);
        await _context.SaveChangesAsync(); // Each item triggers SQL
    }
    
    // ✅ GOOD: Bulk insert (10,000 rooms = fast!)
    public async Task ImportRoomsFastAsync(List<HotelRoom> rooms)
    {
        await _context.BulkInsertAsync(rooms);
    }
    
    // ✅ EXCELLENT: Bulk upsert (update if exists, insert if new)
    public async Task SyncRoomsAsync(List<HotelRoom> rooms)
    {
        await _context.BulkInsertOrUpdateAsync(rooms);
    }
    
    // ✅ PERFORMANCE: Bulk delete with conditions
    public async Task DeleteInactiveRoomsAsync()
    {
        var toDelete = _context.Rooms.Where(r => !r.IsActive).ToList();
        await _context.BulkDeleteAsync(toDelete);
    }
}
```

**Impact:** 10,000 rows insert in **seconds** instead of **minutes**  
**Why Hotels:** Daily imports of thousands of reservations

---

### 3. Data Transformation & Mapping

#### **AutoMapper** (Object Mapping)
```csharp
// Install: dotnet add package AutoMapper
// Purpose: Map Excel rows → Domain models → Database

public class HotelMappingProfile : Profile
{
    public HotelMappingProfile()
    {
        // Excel DTO → Domain Model
        CreateMap<ExcelReservationDto, HotelReservation>()
            .ForMember(dest => dest.CheckInDate, 
                opt => opt.MapFrom(src => DateTime.Parse(src.CheckInDateStr)))
            .ForMember(dest => dest.CheckOutDate, 
                opt => opt.MapFrom(src => DateTime.Parse(src.CheckOutDateStr)))
            .ForMember(dest => dest.TotalPrice, 
                opt => opt.MapFrom(src => src.NightlyRate * src.Nights));
    }
}

// Usage in service
public class ReservationImportService
{
    private readonly IMapper _mapper;
    
    public List<HotelReservation> MapExcelToReservations(List<ExcelReservationDto> excelData)
    {
        return _mapper.Map<List<HotelReservation>>(excelData);
    }
}
```

#### **CsvHelper** (CSV Support)
```csharp
// Install: dotnet add package CsvHelper
// Purpose: Handle CSV exports (hotels often export both Excel and CSV)

public class CsvImportService
{
    public List<HotelReservation> ImportFromCsv(string filePath)
    {
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<HotelReservationMap>();
            return csv.GetRecords<HotelReservation>().ToList();
        }
    }
}
```

#### **Dapper** (High-Performance Queries)
```csharp
// Install: dotnet add package Dapper
// Purpose: Fast SQL queries when EF overhead is too much

public class DapperReportService
{
    private readonly IDbConnection _connection;
    
    public async Task<OccupancyReport> GetOccupancyReportAsync(DateTime date)
    {
        // Raw SQL for pure performance
        var sql = @"
            SELECT 
                COUNT(*) as TotalRooms,
                COUNT(CASE WHEN CheckInDate <= @date AND CheckOutDate > @date THEN 1 END) as OccupiedRooms
            FROM HotelRooms
            WHERE PropertyId = @propertyId";
        
        var result = await _connection.QueryAsync<OccupancyReport>(sql, 
            new { date, propertyId = 1 });
        
        return result.FirstOrDefault();
    }
}
```

---

### 4. Data Validation

#### **FluentValidation** (Hotel-Specific Rules)
```csharp
// Install: dotnet add package FluentValidation
// Purpose: Validate incoming PMS data before database insert

public class HotelReservationValidator : AbstractValidator<HotelReservation>
{
    public HotelReservationValidator()
    {
        // Room validation
        RuleFor(x => x.RoomNumber)
            .NotEmpty().WithMessage("Room number is required")
            .Length(1, 10).WithMessage("Room number must be 1-10 characters");
        
        // Date validation (critical in hospitality)
        RuleFor(x => x.CheckInDate)
            .NotEmpty().WithMessage("Check-in date is required")
            .GreaterThanOrEqualTo(DateTime.UtcNow).WithMessage("Check-in cannot be in the past");
        
        RuleFor(x => x.CheckOutDate)
            .NotEmpty().WithMessage("Check-out date is required")
            .GreaterThan(x => x.CheckInDate).WithMessage("Check-out must be after check-in");
        
        // Rate validation
        RuleFor(x => x.NightlyRate)
            .GreaterThan(0).WithMessage("Nightly rate must be greater than 0")
            .LessThan(10000).WithMessage("Nightly rate seems unreasonably high");
        
        // Guest validation
        RuleFor(x => x.GuestName)
            .NotEmpty().WithMessage("Guest name is required")
            .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("Invalid guest name format");
        
        RuleFor(x => x.GuestEmail)
            .EmailAddress().WithMessage("Invalid email format");
    }
}

// Usage in ETL
public class ValidatingReservationImporter
{
    private readonly IValidator<HotelReservation> _validator;
    private readonly ILogger<ValidatingReservationImporter> _logger;
    
    public async Task<ImportResult> ImportAndValidateAsync(List<HotelReservation> reservations)
    {
        var result = new ImportResult();
        var validReservations = new List<HotelReservation>();
        
        foreach (var reservation in reservations)
        {
            var validation = await _validator.ValidateAsync(reservation);
            
            if (!validation.IsValid)
            {
                // Log EACH failure with details
                foreach (var error in validation.Errors)
                {
                    _logger.LogWarning(
                        "Validation failed for reservation {ReservationId}: {ErrorMessage}",
                        reservation.Id, error.ErrorMessage);
                    result.FailedRows.Add(new FailedRow
                    {
                        ReservationId = reservation.Id,
                        ErrorMessage = error.ErrorMessage,
                        ErrorTimestamp = DateTime.UtcNow
                    });
                }
            }
            else
            {
                validReservations.Add(reservation);
            }
        }
        
        result.SuccessfulImports = validReservations.Count;
        result.FailedImports = reservations.Count - validReservations.Count;
        return result;
    }
}
```

---

### 5. Resilience & Retry Logic

#### **Polly** (Retry Policies)
```csharp
// Install: dotnet add package Polly
// Purpose: Handle unreliable Excel sources or network issues

public class ResilientExcelService
{
    private readonly IAsyncPolicy<List<HotelRoom>> _retryPolicy;
    
    public ResilientExcelService()
    {
        // Retry 3 times with exponential backoff (2s, 4s, 8s)
        _retryPolicy = Policy
            .Handle<IOException>()
            .Or<InvalidOperationException>()
            .OrResult<List<HotelRoom>>(r => r == null)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s");
                });
    }
    
    public async Task<List<HotelRoom>> ReadExcelWithRetryAsync(string filePath)
    {
        return await _retryPolicy.ExecuteAsync(() => ReadExcelAsync(filePath));
    }
}
```

---

### 6. Scheduling (Background Jobs)

#### **Hangfire** (Recurring ETL Runs)
```csharp
// Install: dotnet add package Hangfire.Core
//          dotnet add package Hangfire.SqlServer
// Purpose: Schedule nightly Excel imports

public class HotelEtlScheduler
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly HotelImportService _importService;
    
    public void ScheduleNightlyImports()
    {
        // Run every night at 1 AM
        _recurringJobManager.AddOrUpdate(
            "hotel-nightly-import",
            () => _importService.ImportDailyReservationsAsync(),
            "0 1 * * *"); // Cron: 1 AM every day
        
        // Run every Monday at 2 AM for weekly reports
        _recurringJobManager.AddOrUpdate(
            "hotel-weekly-report",
            () => _importService.GenerateWeeklyReportAsync(),
            "0 2 * * 1"); // Cron: 1 AM every Monday
    }
}

// Setup in Program.cs
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage("DefaultConnection"));
builder.Services.AddHangfireServer();

// Then use it
app.UseHangfireDashboard();
var scheduler = app.Services.GetRequiredService<HotelEtlScheduler>();
scheduler.ScheduleNightlyImports();
```

---

### 7. Logging & Observability

#### **Serilog** (Structured Logging)
```csharp
// Install: dotnet add package Serilog
//          dotnet add package Serilog.AspNetCore
//          dotnet add package Serilog.Sinks.File
//          dotnet add package Serilog.Sinks.Seq
// Purpose: Track row-level failures for audit

// In Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/hotel-pms-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://localhost:5341") // Centralized log server
    .Enrich.FromLogContext()
    .CreateLogger();

// Usage in ETL service
public class LoggedImportService
{
    private readonly ILogger<LoggedImportService> _logger;
    
    public async Task ImportReservationsAsync(string filePath)
    {
        _logger.LogInformation("Starting reservation import from {FilePath}", filePath);
        
        try
        {
            var reservations = await ReadExcelAsync(filePath);
            _logger.LogInformation("Read {Count} reservations from Excel", reservations.Count);
            
            foreach (var reservation in reservations)
            {
                try
                {
                    await InsertReservationAsync(reservation);
                    
                    _logger.LogInformation(
                        "Successfully imported reservation {ReservationId} for guest {GuestName} " +
                        "CheckIn={CheckInDate} CheckOut={CheckOutDate}",
                        reservation.Id, reservation.GuestName, 
                        reservation.CheckInDate, reservation.CheckOutDate);
                }
                catch (Exception ex)
                {
                    // Log EACH failure with context
                    _logger.LogError(ex,
                        "Failed to import reservation {ReservationId} for guest {GuestName}. " +
                        "Room={RoomNumber} CheckIn={CheckInDate}",
                        reservation.Id, reservation.GuestName, 
                        reservation.RoomNumber, reservation.CheckInDate);
                }
            }
            
            _logger.LogInformation("Reservation import completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during reservation import");
            throw;
        }
    }
}
```

---

## 🏨 Hotel PMS Domain Models

```csharp
namespace HotelPMS.Domain;

public class HotelProperty
{
    public int PropertyId { get; set; }
    public string PropertyName { get; set; }
    public string City { get; set; }
    public int TotalRooms { get; set; }
}

public class HotelRoom
{
    public int RoomId { get; set; }
    public int PropertyId { get; set; }
    public string RoomNumber { get; set; }
    public string RoomType { get; set; } // Single, Double, Suite, etc.
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; }
}

public class HotelReservation
{
    public int ReservationId { get; set; }
    public int RoomId { get; set; }
    public string GuestName { get; set; }
    public string GuestEmail { get; set; }
    public string GuestPhone { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal NightlyRate { get; set; }
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ImportedAt { get; set; }
}

public class GuestProfile
{
    public int GuestId { get; set; }
    public string GuestEmail { get; set; }
    public string GuestName { get; set; }
    public string Phone { get; set; }
    public int TotalStays { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime FirstVisit { get; set; }
    public DateTime LastVisit { get; set; }
}

public enum ReservationStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    CheckedOut,
    Cancelled,
    NoShow
}

public class ImportLog
{
    public int LogId { get; set; }
    public string FilePath { get; set; }
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public DateTime ImportStartTime { get; set; }
    public DateTime ImportEndTime { get; set; }
    public string Errors { get; set; }
}
```

---

## 📊 Comparison: Package Selection

| Task | Best Package | Why |
|------|--------------|-----|
| **Read large Excel** | EPPlus | Handles 100k+ rows efficiently |
| **Excel API simplicity** | ClosedXML | Fluent, intuitive |
| **Pure speed** | ExcelDataReader | Fastest for read-only |
| **Fast inserts** | EFCore.BulkExtensions | Critical for 10k+ rows daily |
| **Object mapping** | AutoMapper | Clean, configurable |
| **Data validation** | FluentValidation | Hotel-specific rules |
| **Handle failures** | Polly | Retry with backoff |
| **Schedule ETL** | Hangfire | Dashboard + cron jobs |
| **Track errors** | Serilog | Row-level error tracking |

---

## 🎯 Industry-Specific Pain Points

### Problem 1: Daily Reservation Overload
**Challenge:** 10,000 new reservations daily from multiple properties
**Solution:** EFCore.BulkExtensions for bulk insert in seconds

### Problem 2: Date/Occupancy Conflicts
**Challenge:** Booking the same room for overlapping dates
**Solution:** FluentValidation + database constraints

### Problem 3: Rate Discrepancies
**Challenge:** Base rate vs. special rates vs. channel manager rates
**Solution:** AutoMapper with calculation logic

### Problem 4: Guest No-Shows & Cancellations
**Challenge:** Track reservations changing from Confirmed → NoShow → Cancelled
**Solution:** Serilog logging each state change with timestamp

### Problem 5: Multi-Property Synchronization
**Challenge:** Sync room inventory across 50+ properties
**Solution:** Scheduled Hangfire jobs + structured logging

### Problem 6: Integration with Channel Managers
**Challenge:** Sync with Booking.com, Expedia, Airbnb APIs
**Solution:** Polly retry policies + structured error logging

---

## 🚀 Next: Complete ETL Pipeline

See `HOTEL_PMS_ETL_PIPELINE.md` for full implementation with:
- Excel reader service
- Validation pipeline
- Bulk import engine
- Scheduled jobs
- Error handling
- Audit logging

---

**Hotels process more data per day than most SaaS companies.** These packages exist because the industry demanded them. 🏨
