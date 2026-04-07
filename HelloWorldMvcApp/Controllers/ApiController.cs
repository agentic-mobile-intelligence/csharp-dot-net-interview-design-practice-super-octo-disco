using Microsoft.AspNetCore.Mvc;
using HelloWorldMvcApp.Models;
using HelloWorldMvcApp.Services;

namespace HelloWorldMvcApp.Controllers;

/// <summary>
/// API Controller - Demonstrates REST API with JSON serialization
/// Shows practical use of Newtonsoft.Json and dependency injection
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ApiController : ControllerBase
{
    private readonly IJsonService _jsonService;
    private readonly ILogger<ApiController> _logger;

    public ApiController(IJsonService jsonService, ILogger<ApiController> logger)
    {
        _jsonService = jsonService;
        _logger = logger;
    }

    // ============ GET ENDPOINTS ============

    /// <summary>
    /// GET /api/api/demo
    /// Returns sample data as JSON
    /// </summary>
    [HttpGet("demo")]
    public IActionResult GetDemo()
    {
        _logger.LogInformation("API Demo endpoint accessed");

        var data = new
        {
            id = 1,
            title = "Hello World API",
            description = "Demonstrating Newtonsoft.Json with ASP.NET Core",
            features = new[] { "JSON Serialization", "API Response Formatting", "Error Handling" }
        };

        var response = ApiResponse<dynamic>.Success(data, "Demo data retrieved successfully");
        return Ok(response);
    }

    /// <summary>
    /// GET /api/api/users
    /// Returns paginated user data
    /// </summary>
    [HttpGet("users")]
    public IActionResult GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation($"Get users endpoint accessed - Page: {page}, PageSize: {pageSize}");

        // Simulated data
        var users = new[]
        {
            new { id = 1, name = "John Doe", email = "john@example.com" },
            new { id = 2, name = "Jane Smith", email = "jane@example.com" },
            new { id = 3, name = "Bob Johnson", email = "bob@example.com" }
        };

        var paginatedResponse = new PaginatedResponse<dynamic>
        {
            Items = users.Skip((page - 1) * pageSize).Take(pageSize).ToList<dynamic>(),
            Total = users.Length,
            Page = page,
            PageSize = pageSize
        };

        return Ok(paginatedResponse);
    }

    // ============ POST ENDPOINTS ============

    /// <summary>
    /// POST /api/api/serialize
    /// Demonstrates JSON serialization
    /// </summary>
    [HttpPost("serialize")]
    public IActionResult Serialize([FromBody] dynamic data)
    {
        _logger.LogInformation("Serialize endpoint called");

        try
        {
            // Serialize to JSON
            var json = _jsonService.Serialize(data);
            var prettyJson = _jsonService.SerializePretty(data);

            var result = new
            {
                compact = json,
                pretty = prettyJson
            };

            var response = ApiResponse<dynamic>.Success(result, "Serialization successful");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Serialization failed");
            return BadRequest(ApiResponse<dynamic>.Error("Serialization failed", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/api/deserialize
    /// Demonstrates JSON deserialization
    /// </summary>
    [HttpPost("deserialize")]
    public IActionResult Deserialize([FromBody] DeserializeRequest request)
    {
        _logger.LogInformation("Deserialize endpoint called");

        try
        {
            var deserialized = _jsonService.Deserialize<dynamic>(request.Json ?? "{}");

            var response = ApiResponse<dynamic>.Success(deserialized, "Deserialization successful");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deserialization failed");
            return BadRequest(ApiResponse<dynamic>.Error("Deserialization failed", new List<string> { ex.Message }));
        }
    }

    // ============ ERROR HANDLING ============

    /// <summary>
    /// GET /api/api/error
    /// Demonstrates error response format
    /// </summary>
    [HttpGet("error")]
    public IActionResult GetError()
    {
        _logger.LogWarning("Error endpoint accessed");

        var errors = new List<string>
        {
            "Validation failed",
            "Missing required field: name",
            "Invalid email format"
        };

        return BadRequest(ApiResponse<dynamic>.Error("Request validation failed", errors));
    }

    // ============ HEALTH CHECK ============

    /// <summary>
    /// GET /api/api/health
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        var healthData = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        var response = ApiResponse<dynamic>.Success(healthData, "API is healthy");
        return Ok(response);
    }
}

/// <summary>
/// Request model for deserialization
/// </summary>
public class DeserializeRequest
{
    public string? Json { get; set; }
}
