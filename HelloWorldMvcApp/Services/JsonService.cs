using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HelloWorldMvcApp.Services;

/// <summary>
/// JsonService - Encapsulates JSON serialization/deserialization
/// Demonstrates Newtonsoft.Json (Json.NET) library usage
/// Follows Single Responsibility Principle - only handles JSON operations
/// </summary>
public interface IJsonService
{
    string Serialize<T>(T obj);
    T? Deserialize<T>(string json);
    string SerializePretty<T>(T obj);
}

public class JsonService : IJsonService
{
    // ============ JSON SETTINGS ============

    /// <summary>
    /// Settings for standard JSON serialization
    /// </summary>
    private static readonly JsonSerializerSettings StandardSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatString = "yyyy-MM-dd HH:mm:ss",
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    /// <summary>
    /// Settings for pretty-printed (formatted) JSON
    /// </summary>
    private static readonly JsonSerializerSettings PrettySettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatString = "yyyy-MM-dd HH:mm:ss",
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    // ============ SERIALIZATION (Object → JSON) ============

    /// <summary>
    /// Serialize object to compact JSON string
    /// </summary>
    public string Serialize<T>(T obj)
    {
        try
        {
            return JsonConvert.SerializeObject(obj, StandardSettings);
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to serialize object of type {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Serialize object to pretty-printed JSON string
    /// </summary>
    public string SerializePretty<T>(T obj)
    {
        try
        {
            return JsonConvert.SerializeObject(obj, PrettySettings);
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to serialize object of type {typeof(T).Name}", ex);
        }
    }

    // ============ DESERIALIZATION (JSON → Object) ============

    /// <summary>
    /// Deserialize JSON string to object
    /// </summary>
    public T? Deserialize<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json, StandardSettings);
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to deserialize JSON to type {typeof(T).Name}", ex);
        }
    }
}

/// <summary>
/// Custom exception for JSON operations
/// </summary>
public class JsonException : Exception
{
    public JsonException(string message) : base(message) { }
    public JsonException(string message, Exception innerException) : base(message, innerException) { }
}
