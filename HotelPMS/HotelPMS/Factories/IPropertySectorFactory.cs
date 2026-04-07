using HotelPMS.Calculators;
using HotelPMS.Transformers;
using HotelPMS.Validators;

namespace HotelPMS.Factories;

/// <summary>
/// Abstract Factory: creates a coherent family of objects (validator, transformer, NOI calculator)
/// for a specific property sector. Adding a new sector = one new factory class, zero changes elsewhere.
/// </summary>
public interface IPropertySectorFactory
{
    IValidationStrategy CreateValidator();
    IDataTransformer CreateTransformer();
    INOICalculator CreateNOICalculator();
}
