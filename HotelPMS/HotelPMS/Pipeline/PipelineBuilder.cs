using HotelPMS.Calculators;
using HotelPMS.Domain.Models;
using HotelPMS.Transformers;
using HotelPMS.Validators;

namespace HotelPMS.Pipeline;

/// <summary>
/// Builder pattern: assembles an ETL pipeline fluently.
/// Validates stage ordering so you can't bulk-load before transforming.
/// </summary>
public class PipelineBuilder
{
    private readonly PipelineConfig _config = new();
    private readonly List<string> _stagesAdded = new();

    public PipelineBuilder WithSourceName(string name)
    {
        _config.SourceName = name;
        return this;
    }

    public PipelineBuilder WithValidator(IValidationStrategy validator)
    {
        _config.Validator = validator;
        _stagesAdded.Add("Validator");
        return this;
    }

    public PipelineBuilder WithTransformer(IDataTransformer transformer)
    {
        _config.Transformer = transformer;
        _stagesAdded.Add("Transformer");
        return this;
    }

    public PipelineBuilder WithNOICalculator(INOICalculator calculator)
    {
        _config.NOICalculator = calculator;
        _stagesAdded.Add("NOICalculator");
        return this;
    }

    public PipelineBuilder WithBulkLoader(Func<IEnumerable<DomainProperty>, CancellationToken, Task> loader)
    {
        _config.BulkLoader = loader;
        _stagesAdded.Add("BulkLoader");
        return this;
    }

    public PipelineBuilder WithEventPublisher(Func<NOIResult, CancellationToken, Task> publisher)
    {
        _config.EventPublisher = publisher;
        _stagesAdded.Add("EventPublisher");
        return this;
    }

    /// <summary>
    /// Validates stage ordering and returns the assembled pipeline.
    /// Throws <see cref="InvalidOperationException"/> if stages are misconfigured.
    /// </summary>
    public EtlPipeline Build()
    {
        // You can't bulk-load if there's nothing to transform
        if (_stagesAdded.Contains("BulkLoader") && !_stagesAdded.Contains("Transformer"))
            throw new InvalidOperationException("BulkLoader requires a Transformer to be configured first.");

        // You can't publish an event without a NOI calculation
        if (_stagesAdded.Contains("EventPublisher") && !_stagesAdded.Contains("NOICalculator"))
            throw new InvalidOperationException("EventPublisher requires a NOICalculator to be configured first.");

        Console.WriteLine($"[BUILDER] Pipeline assembled with stages: {string.Join(" → ", _stagesAdded)}");
        return new EtlPipeline(_config);
    }
}
