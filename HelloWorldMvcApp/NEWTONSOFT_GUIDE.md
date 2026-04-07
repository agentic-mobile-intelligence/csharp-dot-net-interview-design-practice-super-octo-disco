# Newtonsoft.Json (Json.NET) Library Guide

## 📚 Overview

Newtonsoft.Json, also known as Json.NET, is the most popular JSON serialization library for .NET. It provides:
- Fast JSON serialization/deserialization
- Flexible configuration options
- LINQ to JSON
- Advanced features like custom converters

This guide shows how to use it in your ASP.NET Core MVC application.

---

## 📦 What's Included

### Added to Project

```xml
<!-- In HelloWorldMvcApp.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
```

### New Files Created

1. **Services/JsonService.cs** - Encapsulation of JSON operations
2. **Models/ApiResponse.cs** - Standard API response formats
3. **Controllers/ApiController.cs** - REST API examples
4. **Updated Program.cs** - Dependency injection registration

---

## 🚀 Quick Start

### 1. Basic Serialization (Object → JSON)

```csharp
using Newtonsoft.Json;

var person = new { Name = "John", Age = 30 };
string json = JsonConvert.SerializeObject(person);
// Output: {"Name":"John","Age":30}
```

### 2. Basic Deserialization (JSON → Object)

```csharp
string json = "{\"Name\":\"John\",\"Age\":30}";
var person = JsonConvert.DeserializeObject<dynamic>(json);
Console.WriteLine(person.Name); // Output: John
```

### 3. Using JsonService in Application

```csharp
// Injected in controller
public class HomeController
{
    private readonly IJsonService _jsonService;
    
    public HomeController(IJsonService jsonService)
    {
        _jsonService = jsonService;
    }
    
    public void Example()
    {
        var data = new { Title = "Hello" };
        string json = _jsonService.Serialize(data);
        string pretty = _jsonService.SerializePretty(data);
    }
}
```

---

## 💡 Common Use Cases

### Use Case 1: API Response Formatting

```csharp
// In ApiController
[HttpGet("users")]
public IActionResult GetUsers()
{
    var data = new[] { /* ... */ };
    var response = ApiResponse<dynamic>.Success(data, "Users retrieved");
    return Ok(response);
}

// Automatically serialized to:
// {
//   "success": true,
//   "message": "Users retrieved",
//   "data": [...],
//   "timestamp": "2024-01-15T10:30:00",
//   "errors": []
// }
```

### Use Case 2: JSON Configuration Files

```csharp
// appsettings.json
{
  "Database": {
    "ConnectionString": "Server=...",
    "Timeout": 30
  },
  "Logging": {
    "LogLevel": "Information"
  }
}

// In application
var dbSettings = configuration.GetSection("Database").Get<DatabaseSettings>();
```

### Use Case 3: Data Transformation

```csharp
// Convert between formats
public string TransformData(object data)
{
    // Serialize then deserialize
    string json = JsonConvert.SerializeObject(data);
    var transformed = JsonConvert.DeserializeObject<dynamic>(json);
    return transformed;
}
```

### Use Case 4: API Integration

```csharp
// Call external API
using (var client = new HttpClient())
{
    var response = await client.GetAsync("https://api.example.com/data");
    var content = await response.Content.ReadAsStringAsync();
    var data = JsonConvert.DeserializeObject<ApiData>(content);
}
```

---

## ⚙️ Configuration Options

### Standard Settings

```csharp
var settings = new JsonSerializerSettings
{
    // Ignore null values
    NullValueHandling = NullValueHandling.Ignore,
    
    // Format dates
    DateFormatString = "yyyy-MM-dd HH:mm:ss",
    
    // Use camelCase for property names
    ContractResolver = new CamelCasePropertyNamesContractResolver(),
    
    // Pretty print output
    Formatting = Formatting.Indented
};

string json = JsonConvert.SerializeObject(obj, settings);
```

### Common Settings

```csharp
// Enum to string
converters = new JsonConverter[] { new StringEnumConverter() }

// Custom date format
DateFormatString = "yyyy-MM-dd"

// Reference handling (circular references)
ReferenceLoopHandling = ReferenceLoopHandling.Ignore

// Type handling
TypeNameHandling = TypeNameHandling.All
```

---

## 🏷️ JSON Attributes

### JsonProperty Attribute

```csharp
public class User
{
    [JsonProperty("full_name")]
    public string Name { get; set; }
    
    [JsonProperty("user_email")]
    public string Email { get; set; }
    
    [JsonIgnore]
    public string Password { get; set; }
}

// Serializes to:
// {
//   "full_name": "John",
//   "user_email": "john@example.com"
//   // Password is ignored
// }
```

### JsonIgnore Attribute

```csharp
[JsonIgnore]
public string SensitiveData { get; set; }
```

### JsonConverter Attribute

```csharp
public class CustomData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public MyEnum Status { get; set; }
}
```

---

## 📋 API Response Patterns

### Pattern 1: Success Response

```csharp
var response = ApiResponse<User>.Success(
    new User { Id = 1, Name = "John" },
    "User retrieved successfully"
);

// JSON:
{
  "success": true,
  "message": "User retrieved successfully",
  "data": {
    "id": 1,
    "name": "John"
  },
  "timestamp": "2024-01-15T10:30:00.000Z",
  "errors": []
}
```

### Pattern 2: Error Response

```csharp
var response = ApiResponse<User>.Error(
    "User not found",
    new List<string> { "User ID does not exist" }
);

// JSON:
{
  "success": false,
  "message": "User not found",
  "data": null,
  "timestamp": "2024-01-15T10:30:00.000Z",
  "errors": ["User ID does not exist"]
}
```

### Pattern 3: Paginated Response

```csharp
var response = new PaginatedResponse<User>
{
    Items = users,
    Total = 100,
    Page = 1,
    PageSize = 10
};

// JSON:
{
  "items": [...],
  "total": 100,
  "page": 1,
  "pageSize": 10,
  "totalPages": 10,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

## 🧪 Testing the API

### Using curl

```bash
# Get demo data
curl https://localhost:7001/api/api/demo

# Get paginated users
curl https://localhost:7001/api/api/users?page=1&pageSize=10

# Serialize data
curl -X POST https://localhost:7001/api/api/serialize \
  -H "Content-Type: application/json" \
  -d '{"name":"John","age":30}'

# Health check
curl https://localhost:7001/api/api/health
```

### Using PowerShell

```powershell
# Get demo data
Invoke-WebRequest https://localhost:7001/api/api/demo | ConvertFrom-Json

# POST request
$data = @{ name = "John"; age = 30 } | ConvertTo-Json
Invoke-WebRequest -Uri https://localhost:7001/api/api/serialize `
    -Method Post `
    -Body $data `
    -ContentType "application/json"
```

### Using Postman

1. Open Postman
2. Create new request
3. Set URL: `https://localhost:7001/api/api/demo`
4. Set method: GET
5. Click Send
6. View formatted JSON response

---

## 🔧 Advanced Features

### Custom Converters

```csharp
public class CustomDateConverter : JsonConverter
{
    public override object? ReadJson(JsonReader reader, Type objectType,
        object? existingValue, JsonSerializer serializer)
    {
        // Custom deserialization logic
        return DateTime.Parse(reader.Value?.ToString() ?? "");
    }

    public override void WriteJson(JsonWriter writer, object? value,
        JsonSerializer serializer)
    {
        // Custom serialization logic
        writer.WriteValue(((DateTime)value!).ToString("yyyy-MM-dd"));
    }
}

// Usage
[JsonConverter(typeof(CustomDateConverter))]
public DateTime MyDate { get; set; }
```

### LINQ to JSON

```csharp
// Parse JSON as JObject
JObject obj = JObject.Parse(jsonString);

// Access properties
string name = obj["person"]["name"].ToString();

// Manipulate
obj["person"]["age"] = 31;

// Convert back to string
string updated = obj.ToString();
```

### Conditional Serialization

```csharp
public class User
{
    public string Name { get; set; }
    
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? MiddleName { get; set; }
}
```

---

## 🎯 Real-World Example: External API Integration

```csharp
public interface IExternalApiService
{
    Task<ApiResponse<T>> FetchDataAsync<T>(string endpoint);
}

public class ExternalApiService : IExternalApiService
{
    private readonly HttpClient _httpClient;
    private readonly IJsonService _jsonService;
    private readonly ILogger<ExternalApiService> _logger;

    public ExternalApiService(HttpClient httpClient, IJsonService jsonService,
        ILogger<ExternalApiService> logger)
    {
        _httpClient = httpClient;
        _jsonService = jsonService;
        _logger = logger;
    }

    public async Task<ApiResponse<T>> FetchDataAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API call failed: {response.StatusCode}");
                return ApiResponse<T>.Error($"API Error: {response.StatusCode}");
            }

            var data = _jsonService.Deserialize<T>(content);
            return ApiResponse<T>.Success(data, "Data fetched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching external data");
            return ApiResponse<T>.Error("Failed to fetch data", new List<string> { ex.Message });
        }
    }
}

// Register in Program.cs
builder.Services.AddHttpClient();
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();

// Use in controller
public class DataController : Controller
{
    private readonly IExternalApiService _apiService;
    
    public DataController(IExternalApiService apiService)
    {
        _apiService = apiService;
    }
    
    public async Task<IActionResult> GetExternalData()
    {
        var result = await _apiService.FetchDataAsync<MyData>(
            "https://api.example.com/data"
        );
        return Ok(result);
    }
}
```

---

## 📊 Performance Tips

### Tip 1: Cache JsonSerializerSettings

```csharp
// ❌ Inefficient - creates new settings each time
public string Serialize(object obj)
{
    var settings = new JsonSerializerSettings { /* ... */ };
    return JsonConvert.SerializeObject(obj, settings);
}

// ✅ Efficient - reuse settings
private static readonly JsonSerializerSettings Settings = 
    new JsonSerializerSettings { /* ... */ };

public string Serialize(object obj)
{
    return JsonConvert.SerializeObject(obj, Settings);
}
```

### Tip 2: Use Streaming for Large Data

```csharp
// For large files
using (var reader = new StreamReader("large-file.json"))
using (var jsonReader = new JsonTextReader(reader))
{
    var serializer = new JsonSerializer();
    var obj = serializer.Deserialize<dynamic>(jsonReader);
}
```

### Tip 3: Configure Once, Reuse

```csharp
// In services
public class JsonService : IJsonService
{
    private static readonly JsonSerializerSettings StandardSettings = 
        new() { /* ... */ };
    
    // Reuse across all methods
}
```

---

## 🎓 Interview Questions

### Q1: "What's the difference between JsonConvert and JsonSerializer?"

**A:** JsonConvert is from Newtonsoft.Json (older, still popular). JsonSerializer is built into .NET Core 3+ (System.Text.Json). Newtonsoft is more feature-rich but JsonSerializer is faster and included by default.

### Q2: "How do you handle circular references in JSON?"

**A:** Use `ReferenceLoopHandling` setting:
```csharp
var settings = new JsonSerializerSettings
{
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
};
```

### Q3: "How would you customize property names in JSON?"

**A:** Use `[JsonProperty]` attribute:
```csharp
[JsonProperty("user_name")]
public string Name { get; set; }
```

### Q4: "How do you handle API versioning with JSON?"

**A:** Use custom converters and conditional serialization:
```csharp
var settings = new JsonSerializerSettings
{
    Converters = new[] { new VersionConverter() }
};
```

---

## 📚 Comparison: Newtonsoft vs System.Text.Json

| Feature | Newtonsoft.Json | System.Text.Json |
|---------|-----------------|-----------------|
| **Performance** | Good | Faster |
| **Features** | Rich | Basic |
| **LINQ to JSON** | Yes | No |
| **Custom Converters** | Easy | More Complex |
| **Configuration** | Extensive | Limited |
| **Nullability** | Flexible | Strict |
| **Included in .NET** | No (NuGet) | Yes (built-in) |

---

## 🔗 API Endpoints Available

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/api/demo` | GET | Get sample demo data |
| `/api/api/users` | GET | Get paginated users |
| `/api/api/serialize` | POST | Serialize data to JSON |
| `/api/api/deserialize` | POST | Deserialize JSON to object |
| `/api/api/error` | GET | See error response format |
| `/api/api/health` | GET | Health check |

---

## ✨ Key Takeaways

1. **Newtonsoft.Json** is industry standard for JSON handling
2. **JsonService** encapsulates JSON operations (SRP principle)
3. **ApiResponse** patterns provide consistent API responses
4. **Attributes** like `[JsonProperty]` customize serialization
5. **Settings** are cached for performance
6. **Dependency Injection** makes services testable and flexible

---

## 📖 Next Steps

1. Run the application: `dotnet run`
2. Test API endpoints:
   - `https://localhost:7001/api/api/demo`
   - `https://localhost:7001/api/api/health`
3. Use Postman to test POST endpoints
4. Review JsonService.cs for implementation
5. Study ApiResponse.cs for response patterns
6. Extend with your own API endpoints

---

**Now you have a complete example of Newtonsoft.Json in production use!** 🚀
