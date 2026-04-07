using System.Diagnostics;
using HotelPMS.Domain.Models;

namespace HotelPMS.Transformers.Decorators;

/// <summary>
/// Decorator: wraps any IDataTransformer and records wall-clock execution time.
/// In production, this would push metrics to Prometheus / Application Insights.
/// </summary>
public class MetricsTransformerDecorator : IDataTransformer
{
    private readonly IDataTransformer _inner;
    private int _totalTransforms;
    private long _totalElapsedMs;

    public MetricsTransformerDecorator(IDataTransformer inner) => _inner = inner;

    public int TotalTransforms => _totalTransforms;
    public double AverageElapsedMs => _totalTransforms == 0 ? 0 : (double)_totalElapsedMs / _totalTransforms;

    public async Task<DomainProperty> TransformAsync(PropertyRow row, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await _inner.TransformAsync(row, ct);
        }
        finally
        {
            sw.Stop();
            Interlocked.Increment(ref _totalTransforms);
            Interlocked.Add(ref _totalElapsedMs, sw.ElapsedMilliseconds);
            Console.WriteLine($"[METRICS] Row {row.SourceRowNumber} transformed in {sw.ElapsedMilliseconds}ms (avg: {AverageElapsedMs:F1}ms over {_totalTransforms} rows)");
        }
    }
}
