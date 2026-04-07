using HotelPMS.Domain.Models;

namespace HotelPMS.Transformers.Decorators;

/// <summary>
/// Decorator: wraps any IDataTransformer and logs before/after each transform.
/// </summary>
public class LoggingTransformerDecorator : IDataTransformer
{
    private readonly IDataTransformer _inner;

    public LoggingTransformerDecorator(IDataTransformer inner) => _inner = inner;

    public async Task<DomainProperty> TransformAsync(PropertyRow row, CancellationToken ct = default)
    {
        Console.WriteLine($"[LOG] Transforming row {row.SourceRowNumber} for property '{row.PropertyId}' ({row.Sector})");
        var result = await _inner.TransformAsync(row, ct);
        Console.WriteLine($"[LOG] Transformed row {row.SourceRowNumber} → NOI ${result.NOI:N2}");
        return result;
    }
}
