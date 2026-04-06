# Multi-Sector Property ETL: Design Patterns for Hotel/Multifamily/Senior Living

## 🏢 The Problem

You're building a Hubtricity-like system for real estate data processing across **three different sectors:**

- **Hotels:** Room types, occupancy rates, nightly rates, check-in/out dates
- **Multifamily:** Units, lease terms, rent rolls, occupancy percentages
- **Senior Living:** Care levels, assisted living costs, occupancy by care type

Each sector has **different validation rules, NOI calculations, and data transformations**, but the core pipeline is similar.

❌ **BAD:** Hard-code if-else chains everywhere
```csharp
if (sector == "hotel") { /* hotel logic */ }
else if (sector == "multifamily") { /* multifamily logic */ }
else if (sector == "senior-living") { /* senior living logic */ }
```

✅ **GOOD:** Use design patterns to make sectors pluggable

---

## 🏗️ Abstract Factory Pattern

**Problem:** You need a cohesive set of objects that work together for each sector.

**Solution:** Define `IPropertySectorFactory` that creates families of related objects.

```csharp
public interface IPropertySectorFactory
{
    IPropertyValidator CreateValidator();
    IPropertyTransformer CreateTransformer();
    INOICalculator CreateNOICalculator();
    IPropertyRepository CreateRepository();
}

// Hotel implementation
public class HotelSectorFactory : IPropertySectorFactory
{
    public IPropertyValidator CreateValidator() => new HotelValidator();
    public IPropertyTransformer CreateTransformer() => new HotelTransformer();
    public INOICalculator CreateNOICalculator() => new HotelNOICalculator();
    public IPropertyRepository CreateRepository() => new HotelRepository();
}

// Multifamily implementation
public class MultifamilySectorFactory : IPropertySectorFactory
{
    public IPropertyValidator CreateValidator() => new MultifamilyValidator();
    public IPropertyTransformer CreateTransformer() => new MultifamilyTransformer();
    public INOICalculator CreateNOICalculator() => new MultifamilyNOICalculator();
    public IPropertyRepository CreateRepository() => new MultifamilyRepository();
}

// Senior Living implementation
public class SeniorLivingSectorFactory : IPropertySectorFactory
{
    public IPropertyValidator CreateValidator() => new SeniorLivingValidator();
    public IPropertyTransformer CreateTransformer() => new SeniorLivingTransformer();
    public INOICalculator CreateNOICalculator() => new SeniorLivingNOICalculator();
    public IPropertyRepository CreateRepository() => new SeniorLivingRepository();
}

// Factory registry
public class SectorFactoryRegistry
{
    private readonly Dictionary<string, IPropertySectorFactory> _factories = new();

    public SectorFactoryRegistry()
    {
        _factories["hotel"] = new HotelSectorFactory();
        _factories["multifamily"] = new MultifamilySectorFactory();
        _factories["senior-living"] = new SeniorLivingSectorFactory();
    }

    public IPropertySectorFactory GetFactory(string sectorType)
    {
        if (!_factories.TryGetValue(sectorType.ToLower(), out var factory))
            throw new ArgumentException($"Unknown sector: {sectorType}");
        return factory;
    }
}
```

**Usage:**

```csharp
public class PropertyImportService
{
    private readonly SectorFactoryRegistry _registry;

    public async Task ImportPropertyDataAsync(string excelFile, string sectorType)
    {
        var factory = _registry.GetFactory(sectorType);

        // Get sector-specific components
        var validator = factory.CreateValidator();
        var transformer = factory.CreateTransformer();
        var calculator = factory.CreateNOICalculator();
        var repository = factory.CreateRepository();

        // All components work together seamlessly
        var data = await ExtractExcelAsync(excelFile);
        var validData = await validator.ValidateAsync(data);
        var transformed = await transformer.TransformAsync(validData);
        var noi = await calculator.CalculateNOIAsync(transformed);
        await repository.SaveAsync(transformed);

        _logger.LogInformation(
            "Successfully imported {Sector} data. NOI: ${NOI}",
            sectorType, noi);
    }
}
```

**Benefits:**
- ✅ No if-else chains scattered through code
- ✅ Adding new sector = create one factory class
- ✅ All components work together by design
- ✅ Easy to test (mock factories)

---

## 🔨 Builder Pattern for ETL Pipelines

**Problem:** Your ETL has many optional stages. Hard to construct without confusing parameter lists.

**Solution:** Use a fluent builder to assemble pipelines.

```csharp
public interface IPipelineStage<TIn, TOut>
{
    Task<TOut> ExecuteAsync(TIn input);
}

public class PropertyEtlPipeline
{
    private List<Func<dynamic, Task<dynamic>>> _stages = new();
    private readonly ILogger<PropertyEtlPipeline> _logger;

    public PropertyEtlPipeline(ILogger<PropertyEtlPipeline> logger)
    {
        _logger = logger;
    }

    // Fluent builder methods
    public PropertyEtlPipeline WithExtractor(IExcelExtractor extractor)
    {
        _stages.Add(async (input) =>
        {
            _logger.LogInformation("Extracting Excel data");
            return await extractor.ExtractAsync(input as string);
        });
        return this;
    }

    public PropertyEtlPipeline WithValidator(IPropertyValidator validator)
    {
        _stages.Add(async (data) =>
        {
            _logger.LogInformation("Validating {Count} rows", (data as List<PropertyRow>)?.Count);
            return await validator.ValidateAsync(data as List<PropertyRow>);
        });
        return this;
    }

    public PropertyEtlPipeline WithTransformer(IPropertyTransformer transformer)
    {
        _stages.Add(async (data) =>
        {
            _logger.LogInformation("Transforming data");
            return await transformer.TransformAsync(data as List<PropertyRow>);
        });
        return this;
    }

    public PropertyEtlPipeline WithEnricher(IPropertyEnricher enricher)
    {
        _stages.Add(async (data) =>
        {
            _logger.LogInformation("Enriching with historical data");
            return await enricher.EnrichAsync(data as List<Property>);
        });
        return this;
    }

    public PropertyEtlPipeline WithNOICalculation(INOICalculator calculator)
    {
        _stages.Add(async (data) =>
        {
            _logger.LogInformation("Calculating NOI");
            return await calculator.CalculateNOIAsync(data as List<Property>);
        });
        return this;
    }

    public PropertyEtlPipeline WithBulkLoader(IBulkLoader loader)
    {
        _stages.Add(async (data) =>
        {
            _logger.LogInformation("Bulk loading to database");
            return await loader.LoadAsync(data as List<Property>);
        });
        return this;
    }

    public PropertyEtlPipeline WithEventPublisher(IEventBus eventBus)
    {
        _stages.Add(async (data) =>
        {
            _logger.LogInformation("Publishing completion event");
            await eventBus.PublishAsync(new ImportCompletedEvent { Data = data });
            return data;
        });
        return this;
    }

    public async Task<dynamic> ExecuteAsync(string excelFilePath)
    {
        dynamic result = excelFilePath;

        foreach (var stage in _stages)
        {
            result = await stage(result);
        }

        return result;
    }

    // Validation: ensure stages are in sensible order
    public void Validate()
    {
        var stageCount = _stages.Count;
        if (stageCount == 0)
            throw new InvalidOperationException("Pipeline has no stages");

        // Could add more sophisticated validation
    }
}
```

**Usage:**

```csharp
var pipeline = new PropertyEtlPipelineBuilder(sector)
    .WithExtractor(excelExtractor)
    .WithValidator(hotelValidator)
    .WithTransformer(hotelTransformer)
    .WithEnricher(historicalDataEnricher)
    .WithNOICalculation(hotelNOICalculator)
    .WithBulkLoader(sqlServerBulkLoader)
    .WithEventPublisher(nOIEventBus)
    .Build();

var result = await pipeline.ExecuteAsync("/data/hotel-reservations.xlsx");

_logger.LogInformation("Pipeline complete: {Result}", result);
```

**Benefits:**
- ✅ Readable, fluent syntax
- ✅ No confusing constructors
- ✅ Optional stages
- ✅ Validates stage order

---

## 🎯 Strategy Pattern for Validation

**Problem:** Different sectors validate differently. Hotels validate room types, multifamily validates unit numbers, etc.

**Solution:** Each sector has a validation strategy.

```csharp
public interface IValidationStrategy
{
    Task<ValidationResult> ValidateAsync(PropertyRow row);
}

// Hotel validation
public class HotelValidationStrategy : IValidationStrategy
{
    public async Task<ValidationResult> ValidateAsync(PropertyRow row)
    {
        var errors = new List<string>();

        // Hotel-specific rules
        if (string.IsNullOrEmpty(row.RoomType))
            errors.Add("Room type is required");

        if (!IsValidRoomType(row.RoomType))
            errors.Add($"Invalid room type: {row.RoomType}");

        if (row.NightlyRate < 10 || row.NightlyRate > 10000)
            errors.Add("Nightly rate must be between $10 and $10,000");

        if (row.CheckOutDate <= row.CheckInDate)
            errors.Add("Checkout must be after check-in");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    private bool IsValidRoomType(string roomType)
    {
        return new[] { "Single", "Double", "Suite", "Deluxe" }.Contains(roomType);
    }
}

// Multifamily validation
public class MultifamilyValidationStrategy : IValidationStrategy
{
    public async Task<ValidationResult> ValidateAsync(PropertyRow row)
    {
        var errors = new List<string>();

        // Multifamily-specific rules
        if (string.IsNullOrEmpty(row.UnitNumber))
            errors.Add("Unit number is required");

        if (row.Rent < 500 || row.Rent > 100000)
            errors.Add("Rent must be between $500 and $100,000");

        if (row.Occupancy < 0 || row.Occupancy > 100)
            errors.Add("Occupancy must be 0-100%");

        if (row.LeaseExpiryDate < DateTime.UtcNow)
            errors.Add("Lease expiry must be in the future");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}

// Senior Living validation
public class SeniorLivingValidationStrategy : IValidationStrategy
{
    public async Task<ValidationResult> ValidateAsync(PropertyRow row)
    {
        var errors = new List<string>();

        // Senior living-specific rules
        if (string.IsNullOrEmpty(row.CareLevel))
            errors.Add("Care level is required");

        if (!IsValidCareLevel(row.CareLevel))
            errors.Add($"Invalid care level: {row.CareLevel}");

        if (row.ResidentAge < 55)
            errors.Add("Residents must be 55+");

        if (row.MonthlyCost < 2000 || row.MonthlyCost > 50000)
            errors.Add("Monthly cost must be reasonable");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    private bool IsValidCareLevel(string careLevel)
    {
        return new[] { "Independent", "Assisted", "Memory Care", "Skilled Nursing" }
            .Contains(careLevel);
    }
}

// Generic validator that uses strategy
public class PropertyRowValidator
{
    private readonly IValidationStrategy _strategy;
    private readonly ILogger<PropertyRowValidator> _logger;

    public PropertyRowValidator(IValidationStrategy strategy, ILogger<PropertyRowValidator> logger)
    {
        _strategy = strategy;
        _logger = logger;
    }

    public async Task<List<PropertyRow>> ValidateAsync(List<PropertyRow> rows)
    {
        var validRows = new List<PropertyRow>();

        foreach (var row in rows)
        {
            var result = await _strategy.ValidateAsync(row);

            if (result.IsValid)
            {
                validRows.Add(row);
            }
            else
            {
                _logger.LogWarning(
                    "Row validation failed: {Errors}",
                    string.Join("; ", result.Errors));
            }
        }

        return validRows;
    }
}
```

**Benefits:**
- ✅ Validation logic specific to each sector
- ✅ Easy to test (mock strategies)
- ✅ Easy to extend (add new sectors)
- ✅ No giant if-else validator

---

## 🎨 Decorator Pattern for Cross-Cutting Concerns

**Problem:** You want retry logic, logging, and metrics on every stage without duplicating code.

**Solution:** Use decorators to wrap stages.

```csharp
public interface IPipelineStage<TIn, TOut>
{
    Task<TOut> ExecuteAsync(TIn input);
}

// Core transformer
public class HotelTransformer : IPipelineStage<List<PropertyRow>, List<Property>>
{
    public async Task<List<Property>> ExecuteAsync(List<PropertyRow> rows)
    {
        // Core transformation logic
        return rows.Select(r => new Property { /* map */ }).ToList();
    }
}

// Logging decorator
public class LoggingTransformerDecorator : IPipelineStage<List<PropertyRow>, List<Property>>
{
    private readonly IPipelineStage<List<PropertyRow>, List<Property>> _inner;
    private readonly ILogger _logger;

    public LoggingTransformerDecorator(
        IPipelineStage<List<PropertyRow>, List<Property>> inner,
        ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<List<Property>> ExecuteAsync(List<PropertyRow> rows)
    {
        _logger.LogInformation("Transformer: Starting with {Count} rows", rows.Count);

        var result = await _inner.ExecuteAsync(rows);

        _logger.LogInformation("Transformer: Completed with {Count} rows", result.Count);
        return result;
    }
}

// Retry decorator
public class RetryTransformerDecorator : IPipelineStage<List<PropertyRow>, List<Property>>
{
    private readonly IPipelineStage<List<PropertyRow>, List<Property>> _inner;
    private readonly int _maxRetries;
    private readonly ILogger _logger;

    public RetryTransformerDecorator(
        IPipelineStage<List<PropertyRow>, List<Property>> inner,
        int maxRetries = 3,
        ILogger logger = null)
    {
        _inner = inner;
        _maxRetries = maxRetries;
        _logger = logger;
    }

    public async Task<List<Property>> ExecuteAsync(List<PropertyRow> rows)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await _inner.ExecuteAsync(rows);
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                _logger?.LogWarning(ex, "Retry attempt {Attempt}", attempt);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }

        throw new InvalidOperationException($"Failed after {_maxRetries} attempts");
    }
}

// Metrics decorator
public class MetricsTransformerDecorator : IPipelineStage<List<PropertyRow>, List<Property>>
{
    private readonly IPipelineStage<List<PropertyRow>, List<Property>> _inner;
    private readonly IMetricsCollector _metrics;

    public MetricsTransformerDecorator(
        IPipelineStage<List<PropertyRow>, List<Property>> inner,
        IMetricsCollector metrics)
    {
        _inner = inner;
        _metrics = metrics;
    }

    public async Task<List<Property>> ExecuteAsync(List<PropertyRow> rows)
    {
        var sw = Stopwatch.StartNew();
        var result = await _inner.ExecuteAsync(rows);
        sw.Stop();

        _metrics.RecordTransformTime(sw.Elapsed);
        _metrics.RecordRowsProcessed(result.Count);

        return result;
    }
}

// Compose them
var transformer = new HotelTransformer();
transformer = new LoggingTransformerDecorator(transformer, logger);
transformer = new RetryTransformerDecorator(transformer, maxRetries: 3, logger);
transformer = new MetricsTransformerDecorator(transformer, metricsCollector);

// Now transformer automatically has logging, retries, and metrics!
var properties = await transformer.ExecuteAsync(rows);
```

**Benefits:**
- ✅ No code duplication
- ✅ Each decorator has one job
- ✅ Compose as needed
- ✅ Works for any stage (validator, transformer, loader)

---

## 📊 Complete Example: Unified Import Service

```csharp
public class UnifiedPropertyImportService
{
    private readonly SectorFactoryRegistry _factoryRegistry;
    private readonly ILogger<UnifiedPropertyImportService> _logger;

    public async Task<ImportResultDto> ImportPropertyDataAsync(
        string excelFile,
        string sectorType)
    {
        var result = new ImportResultDto { SectorType = sectorType };

        try
        {
            // Get sector-specific factory
            var factory = _factoryRegistry.GetFactory(sectorType);

            // Build pipeline with sector-specific components
            var pipeline = new PropertyEtlPipeline(_logger)
                .WithExtractor(new ExcelExtractor())
                .WithValidator(factory.CreateValidator())
                .WithTransformer(factory.CreateTransformer())
                .WithEnricher(new PropertyEnricher())
                .WithNOICalculation(factory.CreateNOICalculator())
                .WithBulkLoader(new SqlServerBulkLoader())
                .WithEventPublisher(new PropertyImportEventBus());

            // Execute
            var properties = await pipeline.ExecuteAsync(excelFile);

            result.SuccessfulRows = properties.Count;
            result.Status = "SUCCESS";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed for sector {Sector}", sectorType);
            result.Status = "FAILED";
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}
```

**This single service handles hotels, multifamily, AND senior living with zero if-else chains.** 🎉

---

## 🎯 Key Takeaways

| Pattern | Purpose | When to Use |
|---------|---------|-------------|
| **Abstract Factory** | Create families of related objects | Multiple sectors with different components |
| **Builder** | Assemble complex objects fluently | Pipelines with many optional stages |
| **Strategy** | Swap algorithms at runtime | Different validation/transformation per sector |
| **Decorator** | Add behavior without modifying | Logging, retries, metrics on any stage |

---

**These patterns are the reason enterprise systems scale to handle multiple property types, sectors, and business rules without becoming unmaintainable spaghetti code.** 🏢
