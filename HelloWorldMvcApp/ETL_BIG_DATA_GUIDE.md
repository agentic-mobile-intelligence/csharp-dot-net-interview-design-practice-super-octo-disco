# ETL & Big Data Processing in C# .NET

## 📚 Overview

**ETL** stands for Extract, Transform, Load - the process of moving data from source to destination through transformation. Essential for:
- Data warehousing
- Business intelligence
- Data migration
- Real-time analytics
- Big data processing

---

## 🔄 ETL Pipeline Architecture

### ETL Flow Diagram

```
Source Systems
   ├── Database
   ├── APIs
   ├── CSV Files
   ├── Cloud Storage
   └── Real-time Streams
        │
        ▼
   ┌─────────────────┐
   │    EXTRACT      │  ← Read data from sources
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │   TRANSFORM     │  ← Clean, validate, enrich
   │  - Validation   │
   │  - Deduplication│
   │  - Enrichment   │
   │  - Aggregation  │
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │      LOAD       │  ← Store in destination
   └────────┬────────┘
            │
            ▼
   Target Systems
   ├── Data Warehouse
   ├── Data Lake
   ├── Analytics DB
   └── Cache Layer
```

---

## 🎯 ETL Service Implementation

### Core Concepts

1. **Extract:** Read from multiple sources
2. **Transform:** Data cleaning and enrichment
3. **Load:** Insert into database
4. **Validate:** Quality checks at each phase
5. **Monitor:** Track progress and errors

### Batch Processing Pattern

```csharp
public class BatchProcessor
{
    private const int BATCH_SIZE = 10000;
    
    public async Task ProcessInBatchesAsync<T>(
        List<T> data,
        Func<List<T>, Task> processBatch
    )
    {
        for (int i = 0; i < data.Count; i += BATCH_SIZE)
        {
            var batch = data.Skip(i).Take(BATCH_SIZE).ToList();
            await processBatch(batch);
            GC.Collect();  // Memory cleanup
        }
    }
}
```

---

## 💡 ETL Examples

### Example 1: CSV to Database

```csharp
public class CsvImportService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task ImportUsersAsync(string filePath)
    {
        // Extract: Read CSV
        var csvLines = await File.ReadAllLinesAsync(filePath);
        var users = new List<User>();
        
        foreach (var line in csvLines.Skip(1))  // Skip header
        {
            var fields = line.Split(',');
            users.Add(new User
            {
                Name = fields[0],
                Email = fields[1],
                CreatedAt = DateTime.Parse(fields[2])
            });
        }
        
        // Validate
        var validUsers = users.Where(u => !string.IsNullOrEmpty(u.Email)).ToList();
        
        // Load: Save to database
        await _unitOfWork.Users.AddRangeAsync(validUsers);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Example 2: API to Database with Transformation

```csharp
public class ApiSyncService
{
    public async Task SyncProductsAsync(string apiUrl)
    {
        using var client = new HttpClient();
        
        // Extract: Fetch from API
        var response = await client.GetAsync(apiUrl);
        var content = await response.Content.ReadAsStringAsync();
        var apiProducts = JsonConvert.DeserializeObject<List<ApiProductDto>>(content);
        
        // Transform: Map to domain model
        var products = apiProducts.Select(api => new Product
        {
            Name = api.ProductName,
            Price = api.Price,
            StockQuantity = api.Stock
        }).ToList();
        
        // Load: Save in batches
        const int batchSize = 1000;
        for (int i = 0; i < products.Count; i += batchSize)
        {
            var batch = products.Skip(i).Take(batchSize);
            await _unitOfWork.Products.AddRangeAsync(batch);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
```

### Example 3: Data Enrichment

```csharp
public class DataEnrichmentService
{
    public List<EnrichedOrder> EnrichOrders(List<Order> orders)
    {
        return orders.Select(o => new EnrichedOrder
        {
            OrderId = o.Id,
            Amount = o.TotalAmount,
            Tax = o.TotalAmount * 0.1m,  // Calculate tax
            Total = o.TotalAmount * 1.1m,  // Calculate total
            Category = o.TotalAmount > 1000 ? "Premium" : "Standard",  // Categorize
            ProcessedAt = DateTime.UtcNow
        }).ToList();
    }
}
```

---

## 🚀 Streaming Large Files

### Memory-Efficient Processing

```csharp
public class LargeFileProcessor
{
    public async Task ProcessLargeJsonFileAsync<T>(
        string filePath,
        Func<T, Task> processRecord
    ) where T : class
    {
        using (var reader = new StreamReader(filePath))
        using (var jsonReader = new JsonTextReader(reader))
        {
            var serializer = JsonSerializer.Create();
            
            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    var record = serializer.Deserialize<T>(jsonReader);
                    if (record != null)
                        await processRecord(record);
                }
            }
        }
    }
}
```

---

## ⚡ Parallel Processing for Performance

### Parallel Transformation

```csharp
public class ParallelDataProcessor
{
    public List<TOutput> TransformInParallel<TInput, TOutput>(
        List<TInput> input,
        Func<TInput, TOutput> transformer
    )
    {
        var result = new List<TOutput>();
        var lockObj = new object();
        
        Parallel.ForEach(
            input,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            item =>
            {
                var transformed = transformer(item);
                lock (lockObj)
                {
                    result.Add(transformed);
                }
            }
        );
        
        return result;
    }
}
```

---

## 📊 Data Validation

### Quality Checks

```csharp
public class DataValidator
{
    public ValidationResult ValidateData<T>(List<T> data) where T : class
    {
        var result = new ValidationResult
        {
            TotalRecords = data.Count
        };
        
        // Check 1: No empty data
        if (!data.Any())
        {
            result.Errors.Add("No data to process");
            return result;
        }
        
        // Check 2: No duplicates
        var duplicateCount = data.GroupBy(x => x).Count(g => g.Count() > 1);
        if (duplicateCount > 0)
            result.Errors.Add($"Found {duplicateCount} duplicate records");
        
        result.IsValid = !result.Errors.Any();
        result.ValidRecords = data.Count - result.Errors.Count;
        
        return result;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public List<string> Errors { get; set; } = new();
}
```

---

## 🔄 Incremental Loading

### Delta Processing

```csharp
public class IncrementalLoadService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task LoadIncrementalAsync(
        List<User> newUsers,
        DateTime lastRunTime
    )
    {
        // Filter: Only new/modified records
        var changes = newUsers.Where(u => u.CreatedAt > lastRunTime).ToList();
        
        // Deduplicate
        var existing = await _unitOfWork.Users.FindAsync(u =>
            changes.Select(c => c.Email).Contains(u.Email)
        );
        var existingEmails = existing.Select(e => e.Email).ToHashSet();
        
        // Load only new records
        var toInsert = changes.Where(c => !existingEmails.Contains(c.Email)).ToList();
        await _unitOfWork.Users.AddRangeAsync(toInsert);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

---

## 📈 Monitoring & Logging

### ETL Progress Tracking

```csharp
public class EtlProgressTracker
{
    private readonly ILogger _logger;
    private DateTime _startTime;
    
    public void Start()
    {
        _startTime = DateTime.UtcNow;
        _logger.LogInformation("ETL started");
    }
    
    public void LogProgress(int processed, int total)
    {
        var elapsed = DateTime.UtcNow - _startTime;
        var rate = processed / elapsed.TotalSeconds;
        var remaining = (total - processed) / rate;
        
        _logger.LogInformation(
            $"Progress: {processed}/{total} ({(processed/total)*100:F1}%) " +
            $"- Rate: {rate:F0}/sec - ETA: {remaining:F0}s"
        );
    }
    
    public void Complete()
    {
        var duration = DateTime.UtcNow - _startTime;
        _logger.LogInformation($"ETL completed in {duration.TotalSeconds:F2}s");
    }
}
```

---

## 🛡️ Error Handling

### Retry with Exponential Backoff

```csharp
public class EtlErrorHandler
{
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3
    )
    {
        int attempt = 0;
        
        while (attempt < maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                attempt++;
                
                if (attempt >= maxRetries)
                    throw;
                
                var delayMs = (int)Math.Pow(2, attempt) * 1000;
                await Task.Delay(delayMs);
            }
        }
        
        throw new InvalidOperationException("Operation failed after retries");
    }
}
```

---

## 🎯 Complete ETL Pipeline Example

### Full Implementation

```csharp
public class CompleteEtlPipeline
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteEtlPipeline> _logger;
    private readonly EtlProgressTracker _tracker;
    
    public async Task ExecuteAsync(string sourceFile)
    {
        _tracker.Start();
        
        try
        {
            // Phase 1: Extract
            _logger.LogInformation("Phase 1: Extracting data");
            var rawData = await ExtractDataAsync(sourceFile);
            _logger.LogInformation($"Extracted {rawData.Count} records");
            
            // Phase 2: Validate
            _logger.LogInformation("Phase 2: Validating data");
            var validation = ValidateData(rawData);
            if (!validation.IsValid)
                throw new Exception($"Validation failed: {string.Join(", ", validation.Errors)}");
            
            // Phase 3: Transform
            _logger.LogInformation("Phase 3: Transforming data");
            var transformed = TransformData(rawData);
            
            // Phase 4: Load
            _logger.LogInformation("Phase 4: Loading data");
            await LoadDataInBatchesAsync(transformed);
            
            _tracker.Complete();
            _logger.LogInformation("ETL pipeline completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL pipeline failed");
            throw;
        }
    }
    
    private async Task<List<SourceUser>> ExtractDataAsync(string file)
    {
        var json = await File.ReadAllTextAsync(file);
        return JsonConvert.DeserializeObject<List<SourceUser>>(json) ?? new();
    }
    
    private ValidationResult ValidateData(List<SourceUser> data)
    {
        var result = new ValidationResult { TotalRecords = data.Count };
        
        if (!data.Any())
            result.Errors.Add("No data");
        
        result.IsValid = !result.Errors.Any();
        return result;
    }
    
    private List<User> TransformData(List<SourceUser> source)
    {
        return source.Select(s => new User
        {
            Name = s.Name?.Trim().ToUpper() ?? "",
            Email = s.Email?.Trim().ToLower() ?? "",
            CreatedAt = DateTime.UtcNow
        }).ToList();
    }
    
    private async Task LoadDataInBatchesAsync(List<User> data)
    {
        const int batchSize = 1000;
        
        for (int i = 0; i < data.Count; i += batchSize)
        {
            var batch = data.Skip(i).Take(batchSize).ToList();
            await _unitOfWork.Users.AddRangeAsync(batch);
            await _unitOfWork.SaveChangesAsync();
            
            _tracker.LogProgress(i + batch.Count, data.Count);
        }
    }
}
```

---

## 🎓 Interview Talking Points

1. **ETL Definition:** Extract data → Transform → Load
2. **Batch Processing:** Handle large data in chunks
3. **Validation:** Quality checks at each phase
4. **Performance:** Use parallel processing, streaming
5. **Error Handling:** Retry logic, logging
6. **Monitoring:** Track progress, duration, errors

---

## 📚 Key Takeaways

- ✅ **Batch Processing:** Prevents memory issues
- ✅ **Validation:** Ensures data quality
- ✅ **Incremental Loading:** Only sync changed data
- ✅ **Error Handling:** Retry with backoff
- ✅ **Logging:** Track every phase
- ✅ **Performance:** Profile and optimize

---

**ETL is critical for modern data-driven applications!** 🚀
