using HotelPMS.Calculators;
using HotelPMS.Domain.Models;
using HotelPMS.Transformers;
using HotelPMS.Validators;

namespace HotelPMS.Pipeline;

/// <summary>
/// The assembled ETL pipeline. Built exclusively via PipelineBuilder.
/// Each stage is optional and skipped when not configured.
/// </summary>
public class EtlPipeline
{
    private readonly PipelineConfig _config;

    internal EtlPipeline(PipelineConfig config) => _config = config;

    public async Task<EtlResult> ExecuteAsync(
        IEnumerable<PropertyRow> rows,
        CancellationToken ct = default)
    {
        var result = new EtlResult();
        var rowList = rows.ToList();

        Console.WriteLine($"[PIPELINE] Starting. Source: '{_config.SourceName}', rows: {rowList.Count}");

        // ── Stage 1: Validate ────────────────────────────────────────────────
        var validRows = new List<PropertyRow>();
        if (_config.Validator is not null)
        {
            foreach (var row in rowList)
            {
                var validation = _config.Validator.Validate(row);
                if (validation.IsValid)
                {
                    validRows.Add(row);
                }
                else
                {
                    result.InvalidRows++;
                    result.ValidationErrors.Add((row.SourceRowNumber, validation.Errors));
                    Console.WriteLine($"[PIPELINE] Row {row.SourceRowNumber} invalid: {string.Join("; ", validation.Errors)}");
                }
            }
        }
        else
        {
            validRows = rowList;
        }

        Console.WriteLine($"[PIPELINE] Validation complete. Valid: {validRows.Count}, Invalid: {result.InvalidRows}");

        // ── Stage 2: Transform ───────────────────────────────────────────────
        var domainObjects = new List<DomainProperty>();
        if (_config.Transformer is not null)
        {
            foreach (var row in validRows)
            {
                var domain = await _config.Transformer.TransformAsync(row, ct);
                domainObjects.Add(domain);
            }
        }

        result.TransformedCount = domainObjects.Count;
        Console.WriteLine($"[PIPELINE] Transformation complete. Transformed: {result.TransformedCount}");

        // ── Stage 3: Calculate NOI ───────────────────────────────────────────
        if (_config.NOICalculator is not null && validRows.Count > 0)
        {
            result.NOIResult = _config.NOICalculator.Calculate(validRows);
            Console.WriteLine($"[PIPELINE] NOI calculated: ${result.NOIResult.NOI:N2} (margin: {result.NOIResult.NOIMargin:P1})");
        }

        // ── Stage 4: Bulk Load ───────────────────────────────────────────────
        if (_config.BulkLoader is not null && domainObjects.Count > 0)
        {
            await _config.BulkLoader(domainObjects, ct);
            result.LoadedCount = domainObjects.Count;
            Console.WriteLine($"[PIPELINE] Bulk load complete. Loaded: {result.LoadedCount} rows");
        }

        // ── Stage 5: Publish Event ───────────────────────────────────────────
        if (_config.EventPublisher is not null && result.NOIResult is not null)
        {
            await _config.EventPublisher(result.NOIResult, ct);
            Console.WriteLine("[PIPELINE] NOI event published.");
        }

        Console.WriteLine("[PIPELINE] Complete.");
        return result;
    }
}

public class EtlResult
{
    public int InvalidRows { get; set; }
    public int TransformedCount { get; set; }
    public int LoadedCount { get; set; }
    public NOIResult? NOIResult { get; set; }
    public List<(int RowNumber, List<string> Errors)> ValidationErrors { get; } = new();
}

/// <summary>Internal config bag populated by the builder.</summary>
internal class PipelineConfig
{
    public string SourceName { get; set; } = "unknown";
    public IValidationStrategy? Validator { get; set; }
    public IDataTransformer? Transformer { get; set; }
    public INOICalculator? NOICalculator { get; set; }
    public Func<IEnumerable<DomainProperty>, CancellationToken, Task>? BulkLoader { get; set; }
    public Func<NOIResult, CancellationToken, Task>? EventPublisher { get; set; }
}
