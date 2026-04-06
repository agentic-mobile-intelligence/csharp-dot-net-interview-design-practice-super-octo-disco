using HotelPMS.Calculators;
using HotelPMS.Transformers;
using HotelPMS.Transformers.Decorators;
using HotelPMS.Validators;

namespace HotelPMS.Factories;

public class HotelSectorFactory : IPropertySectorFactory
{
    private readonly bool _withDecorators;

    /// <param name="withDecorators">
    /// When true, wraps the transformer with logging, retry, and metrics decorators —
    /// demonstrating how the Decorator pattern composes with the Abstract Factory.
    /// </param>
    public HotelSectorFactory(bool withDecorators = true) => _withDecorators = withDecorators;

    public IValidationStrategy CreateValidator() => new HotelValidationStrategy();

    public IDataTransformer CreateTransformer()
    {
        IDataTransformer transformer = new HotelTransformer();

        if (!_withDecorators)
            return transformer;

        // Compose decorators: innermost = core logic, outermost = first to execute
        transformer = new RetryTransformerDecorator(transformer, maxRetries: 3);
        transformer = new MetricsTransformerDecorator(transformer);
        transformer = new LoggingTransformerDecorator(transformer);
        return transformer;
    }

    public INOICalculator CreateNOICalculator() => new HotelNOICalculator();
}
