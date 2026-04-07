using HotelPMS.Domain.Enums;
using HotelPMS.Domain.Models;
using HotelPMS.Factories;
using HotelPMS.Pipeline;
using HotelPMS.Reconciliation;
using HotelPMS.Repository;

Console.WriteLine("=============================================================");
Console.WriteLine("  HotelPMS Design Patterns Demo — Hubtricity-like PMS System");
Console.WriteLine("=============================================================");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// 1. ABSTRACT FACTORY — resolve a full pipeline family for Hotel sector
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── 1. Abstract Factory ──────────────────────────────────────");
Console.WriteLine("Resolve a coherent family of objects (Validator + Transformer");
Console.WriteLine("+ NOICalculator) for the Hotel sector from a single factory.");
Console.WriteLine();

var hotelFactory = PropertySectorFactoryResolver.Resolve(PropertySector.Hotel, withDecorators: false);
var validator    = hotelFactory.CreateValidator();
var transformer  = hotelFactory.CreateTransformer();
var noiCalc      = hotelFactory.CreateNOICalculator();

Console.WriteLine($"Factory type : {hotelFactory.GetType().Name}");
Console.WriteLine($"Validator    : {validator.GetType().Name}");
Console.WriteLine($"Transformer  : {transformer.GetType().Name}");
Console.WriteLine($"NOI Calc     : {noiCalc.GetType().Name}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// 2. STRATEGY — validate rows with the sector-specific strategy
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── 2. Strategy Pattern (Validation) ─────────────────────────");
Console.WriteLine("Each sector validates with its own rules. The caller just calls");
Console.WriteLine("Validate() — no if-else chains anywhere.");
Console.WriteLine();

var validRow = new PropertyRow
{
    PropertyId    = "HTL-001",
    Sector        = PropertySector.Hotel,
    ReportDate    = new DateOnly(2026, 3, 31),
    RoomType      = "Standard",
    TotalRooms    = 100,
    OccupiedRooms = 72,
    RoomRevenue   = 14_400m,
    OperatingExpenses = 8_000m,
    SourceRowNumber = 1
};

var invalidRow = new PropertyRow
{
    PropertyId    = "HTL-001",
    Sector        = PropertySector.Hotel,
    ReportDate    = new DateOnly(2026, 3, 31),
    RoomType      = "PentHouse", // wrong capitalisation
    TotalRooms    = 50,
    OccupiedRooms = 75,          // more than total — invalid
    RoomRevenue   = -500m,       // negative — invalid
    SourceRowNumber = 2
};

var validationOk  = validator.Validate(validRow);
var validationBad = validator.Validate(invalidRow);

Console.WriteLine($"Row 1 valid: {validationOk.IsValid}");
Console.WriteLine($"Row 2 valid: {validationBad.IsValid}");
foreach (var err in validationBad.Errors)
    Console.WriteLine($"  ERROR: {err}");
foreach (var warn in validationBad.Warnings)
    Console.WriteLine($"  WARN : {warn}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// 3. DECORATOR — compose logging + retry + metrics around a transformer
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── 3. Decorator Pattern (Pipeline Stages) ───────────────────");
Console.WriteLine("Add cross-cutting concerns (logging, retry, metrics) without");
Console.WriteLine("modifying the core transformer. Compose them like Russian dolls.");
Console.WriteLine();

var decoratedFactory     = PropertySectorFactoryResolver.Resolve(PropertySector.Hotel, withDecorators: true);
var decoratedTransformer = decoratedFactory.CreateTransformer();
Console.WriteLine($"Outermost decorator: {decoratedTransformer.GetType().Name}");
var domain = await decoratedTransformer.TransformAsync(validRow);
Console.WriteLine($"Transformed NOI: ${domain.NOI:N2}, Occupancy: {domain.OccupancyRate:P1}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// 4. BUILDER — assemble a full ETL pipeline fluently
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── 4. Builder Pattern (ETL Pipeline) ────────────────────────");
Console.WriteLine("Assemble the pipeline stage-by-stage. The builder validates");
Console.WriteLine("ordering (e.g. can't bulk-load before transforming).");
Console.WriteLine();

var unitOfWork = new InMemoryUnitOfWork();

var pipeline = new PipelineBuilder()
    .WithSourceName("reservations_march_2026.xlsx")
    .WithValidator(hotelFactory.CreateValidator())
    .WithTransformer(hotelFactory.CreateTransformer())
    .WithNOICalculator(hotelFactory.CreateNOICalculator())
    .WithBulkLoader(async (rows, ct) =>
    {
        unitOfWork.StageRange(rows);
        await unitOfWork.CommitAsync(ct);
    })
    .WithEventPublisher(async (noi, ct) =>
    {
        Console.WriteLine($"[EVENT BUS] Published NOI event: PropertyId={noi.PropertyId}, NOI=${noi.NOI:N2}");
        await Task.CompletedTask;
    })
    .Build();

var rows = new List<PropertyRow> { validRow, invalidRow };
var etlResult = await pipeline.ExecuteAsync(rows);

Console.WriteLine($"ETL Summary → Valid: {etlResult.TransformedCount}, Invalid: {etlResult.InvalidRows}, Loaded: {etlResult.LoadedCount}");
if (etlResult.NOIResult is not null)
    Console.WriteLine($"ETL NOI Result → Total Revenue: ${etlResult.NOIResult.TotalRevenue:N2}, NOI: ${etlResult.NOIResult.NOI:N2}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// 5. REPOSITORY + UNIT OF WORK — query persisted domain objects
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── 5. Repository + Unit of Work ─────────────────────────────");
Console.WriteLine("All writes go through the UoW as one atomic batch.");
Console.WriteLine("The repository exposes sector- and date-aware queries.");
Console.WriteLine();

var allProperties = await unitOfWork.DomainProperties.GetAllAsync();
Console.WriteLine($"Persisted properties: {allProperties.Count}");

var hotelProperties = await unitOfWork.DomainProperties.GetBySectorAsync(PropertySector.Hotel);
Console.WriteLine($"Hotel sector rows   : {hotelProperties.Count}");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// 6. RECONCILIATION SERVICE — pull-model with audit trail
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── 6. Reconciliation Service (Pull Model) ───────────────────");
Console.WriteLine("Hubtricity pulls read-only from the PMS on a schedule.");
Console.WriteLine("Diffs the operational snapshot against the scorecard.");
Console.WriteLine("Flags conflicts for human review; applies clean deltas.");
Console.WriteLine();

var reconciler = new ReconciliationService(
    pmsSource:  new StubPmsDataSource(),
    unitOfWork: new InMemoryUnitOfWork()   // fresh UoW for reconciliation run
);

var reconResult = await reconciler.ReconcileAsync(
    propertyId: "HTL-001",
    reportDate: new DateOnly(2026, 3, 31)
);

Console.WriteLine($"Reconciliation status : {reconResult.Status}");
Console.WriteLine($"New rows              : {reconResult.NewRows}");
Console.WriteLine($"Modified rows         : {reconResult.ModifiedRows}");
Console.WriteLine($"Conflicts (need human): {reconResult.Conflicts.Count}");
Console.WriteLine();

Console.WriteLine("=============================================================");
Console.WriteLine("  All patterns demonstrated successfully.");
Console.WriteLine("=============================================================");
