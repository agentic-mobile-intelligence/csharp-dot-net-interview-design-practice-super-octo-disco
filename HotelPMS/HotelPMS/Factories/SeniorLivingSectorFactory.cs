using HotelPMS.Calculators;
using HotelPMS.Transformers;
using HotelPMS.Transformers.Decorators;
using HotelPMS.Validators;

namespace HotelPMS.Factories;

public class SeniorLivingSectorFactory : IPropertySectorFactory
{
    private readonly bool _withDecorators;

    public SeniorLivingSectorFactory(bool withDecorators = true) => _withDecorators = withDecorators;

    public IValidationStrategy CreateValidator() => new SeniorLivingValidationStrategy();

    public IDataTransformer CreateTransformer()
    {
        IDataTransformer transformer = new SeniorLivingTransformer();

        if (!_withDecorators)
            return transformer;

        transformer = new RetryTransformerDecorator(transformer, maxRetries: 3);
        transformer = new MetricsTransformerDecorator(transformer);
        transformer = new LoggingTransformerDecorator(transformer);
        return transformer;
    }

    public INOICalculator CreateNOICalculator() => new SeniorLivingNOICalculator();
}
