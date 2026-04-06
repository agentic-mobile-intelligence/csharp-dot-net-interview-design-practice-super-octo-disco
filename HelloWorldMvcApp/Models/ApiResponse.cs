using Newtonsoft.Json;

namespace HelloWorldMvcApp.Models;

/// <summary>
/// Standard API response format
/// Demonstrates how Newtonsoft.Json attributes work
/// </summary>
public class ApiResponse<T>
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("data")]
    public T? Data { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty("errors")]
    public List<string> Errors { get; set; } = new();

    // ============ FACTORY METHODS ============

    /// <summary>
    /// Create successful response
    /// </summary>
    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Create error response
    /// </summary>
    public static ApiResponse<T> Error(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new()
        };
    }
}

/// <summary>
/// Generic API response without data
/// </summary>
public class ApiResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty("errors")]
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Paginated response
/// </summary>
public class PaginatedResponse<T>
{
    [JsonProperty("items")]
    public List<T> Items { get; set; } = new();

    [JsonProperty("total")]
    public int Total { get; set; }

    [JsonProperty("page")]
    public int Page { get; set; }

    [JsonProperty("pageSize")]
    public int PageSize { get; set; }

    [JsonProperty("totalPages")]
    public int TotalPages => (Total + PageSize - 1) / PageSize;

    [JsonProperty("hasNextPage")]
    public bool HasNextPage => Page < TotalPages;

    [JsonProperty("hasPreviousPage")]
    public bool HasPreviousPage => Page > 1;
}
