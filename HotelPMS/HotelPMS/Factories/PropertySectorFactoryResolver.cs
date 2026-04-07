using HotelPMS.Domain.Enums;

namespace HotelPMS.Factories;

/// <summary>
/// Resolves the correct factory for a given sector tag.
/// This is the single switch point — the rest of the codebase never inspects the sector enum.
/// </summary>
public static class PropertySectorFactoryResolver
{
    public static IPropertySectorFactory Resolve(PropertySector sector, bool withDecorators = true) =>
        sector switch
        {
            PropertySector.Hotel        => new HotelSectorFactory(withDecorators),
            PropertySector.Multifamily  => new MultifamilySectorFactory(withDecorators),
            PropertySector.SeniorLiving => new SeniorLivingSectorFactory(withDecorators),
            _ => throw new NotSupportedException($"No factory registered for sector '{sector}'.")
        };
}
