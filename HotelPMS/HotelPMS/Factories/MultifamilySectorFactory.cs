using HotelPMS.Calculators;
using HotelPMS.Transformers;
using HotelPMS.Transformers.Decorators;
using HotelPMS.Validators;

namespace HotelPMS.Factories;

public class MultifamilySectorFactory : IPropertySectorFactory
{
    private readonly bool _withDecorators;

    public MultifamilySectorFactory(bool withDecorators = true) => _withDecorators = withDecorators;

    public IValidationStrategy CreateValidator() => new MultifamilyValidationStrategy();

    public IDataTransformer CreateTransformer()
    {
        IDataTransformer transformer = new MultifamilyTransformer();

        if (!_withDecorators)
            return transformer;

        transformer = new RetryTransformerDecorator(transformer, maxRetries: 3);
        transformer = new MetricsTransformerDecorator(transformer);
        transformer = new LoggingTransformerDecorator(transformer);
        return transformer;
    }

    public INOICalculator CreateNOICalculator() => new MultifamilyNOICalculator();
}
