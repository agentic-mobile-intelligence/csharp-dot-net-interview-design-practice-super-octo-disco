using HotelPMS.Domain.Models;

namespace HotelPMS.Transformers;

/// <summary>
/// Transforms raw PropertyRow data into a validated DomainProperty.
/// Implementations are composed via the Decorator pattern.
/// </summary>
public interface IDataTransformer
{
    Task<DomainProperty> TransformAsync(PropertyRow row, CancellationToken ct = default);
}
