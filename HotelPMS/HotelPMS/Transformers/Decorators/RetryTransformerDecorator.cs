using HotelPMS.Domain.Models;

namespace HotelPMS.Transformers.Decorators;

/// <summary>
/// Decorator: wraps any IDataTransformer and retries on transient failures
/// with exponential back-off (useful when the transformer calls an external enrichment API).
/// </summary>
public class RetryTransformerDecorator : IDataTransformer
{
    private readonly IDataTransformer _inner;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;

    public RetryTransformerDecorator(
        IDataTransformer inner,
        int maxRetries = 3,
        TimeSpan? initialDelay = null)
    {
        _inner = inner;
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(200);
    }

    public async Task<DomainProperty> TransformAsync(PropertyRow row, CancellationToken ct = default)
    {
        var delay = _initialDelay;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await _inner.TransformAsync(row, ct);
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                Console.WriteLine($"[RETRY] Attempt {attempt}/{_maxRetries} failed for row {row.SourceRowNumber}: {ex.Message}. Retrying in {delay.TotalMilliseconds}ms...");
                await Task.Delay(delay, ct);
                delay *= 2; // exponential back-off
            }
        }

        // Final attempt — let the exception propagate
        return await _inner.TransformAsync(row, ct);
    }
}
