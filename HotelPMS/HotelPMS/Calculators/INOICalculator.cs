using HotelPMS.Domain.Models;

namespace HotelPMS.Calculators;

public interface INOICalculator
{
    NOIResult Calculate(IEnumerable<PropertyRow> rows);
}
