# DRY & YAGNI Principles in C#

Two fundamental principles that help you write cleaner, more focused code.

---

## DRY: Don't Repeat Yourself

**Definition:** Every piece of knowledge must have a single, unambiguous representation within a system.

### The Problem

```csharp
// ❌ BAD - Code duplication

public class UserService
{
    public bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        if (!email.Contains("@"))
            return false;
            
        if (!email.Contains("."))
            return false;
            
        return true;
    }
}

public class AuthenticationService
{
    public bool IsValidEmail(string email)
    {
        // Same validation logic repeated!
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        if (!email.Contains("@"))
            return false;
            
        if (!email.Contains("."))
            return false;
            
        return true;
    }
}

public class NotificationService
{
    public void SendNotification(string email)
    {
        // And again!
        if (string.IsNullOrWhiteSpace(email))
            return;
        
        if (!email.Contains("@") || !email.Contains("."))
            return;
            
        // Send notification
    }
}
```

### The Solution

```csharp
// ✅ GOOD - Single source of truth

public static class EmailValidator
{
    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        if (!email.Contains("@"))
            return false;
            
        if (!email.Contains("."))
            return false;
            
        return true;
    }
}

public class UserService
{
    public bool ValidateEmail(string email) => EmailValidator.IsValid(email);
}

public class AuthenticationService
{
    public bool IsValidEmail(string email) => EmailValidator.IsValid(email);
}

public class NotificationService
{
    public void SendNotification(string email)
    {
        if (EmailValidator.IsValid(email))
        {
            // Send notification
        }
    }
}
```

### DRY in Different Contexts

#### 1. **Code Duplication**

```csharp
// ❌ BAD
public decimal CalculateDiscount(decimal price, int quantity)
{
    decimal discount = 0;
    if (quantity > 100) discount = price * 0.20m;
    else if (quantity > 50) discount = price * 0.15m;
    else if (quantity > 10) discount = price * 0.10m;
    return discount;
}

public decimal CalculateShippingDiscount(decimal price, int quantity)
{
    decimal discount = 0;
    if (quantity > 100) discount = price * 0.20m;
    else if (quantity > 50) discount = price * 0.15m;
    else if (quantity > 10) discount = price * 0.10m;
    return discount;
}

// ✅ GOOD
public static class DiscountCalculator
{
    public static decimal Calculate(int quantity)
    {
        return quantity switch
        {
            > 100 => 0.20m,
            > 50 => 0.15m,
            > 10 => 0.10m,
            _ => 0.0m
        };
    }
}

public decimal CalculateDiscount(decimal price, int quantity)
    => price * DiscountCalculator.Calculate(quantity);

public decimal CalculateShippingDiscount(decimal price, int quantity)
    => price * DiscountCalculator.Calculate(quantity);
```

#### 2. **Configuration Duplication**

```csharp
// ❌ BAD
public class PaymentService
{
    private const string API_KEY = "secret-key-12345";
    private const string BASE_URL = "https://api.payment.com";
}

public class NotificationService
{
    private const string API_KEY = "secret-key-12345";
    private const string BASE_URL = "https://api.payment.com";
}

// ✅ GOOD
public static class AppConfiguration
{
    public const string PaymentApiKey = "secret-key-12345";
    public const string PaymentBaseUrl = "https://api.payment.com";
}

public class PaymentService
{
    private readonly string _apiKey = AppConfiguration.PaymentApiKey;
    private readonly string _baseUrl = AppConfiguration.PaymentBaseUrl;
}

public class NotificationService
{
    private readonly string _apiKey = AppConfiguration.PaymentApiKey;
    private readonly string _baseUrl = AppConfiguration.PaymentBaseUrl;
}
```

#### 3. **Logic Duplication**

```csharp
// ❌ BAD - Calculating total price in multiple places
public decimal OrderTotal { get; set; }
public decimal OrderTax { get; set; }

public void PlaceOrder()
{
    decimal total = OrderTotal + OrderTax;
    // Save order with total
}

public decimal GetOrderAmount()
{
    return OrderTotal + OrderTax; // Same calculation!
}

// ✅ GOOD
public decimal Total => OrderTotal + OrderTax;

public void PlaceOrder()
{
    // Save order with Total property
}

public decimal GetOrderAmount() => Total;
```

### Benefits of DRY

✅ Single source of truth - changes in one place  
✅ Easier maintenance - fix bugs once, not multiple times  
✅ Reduced errors - consistent behavior everywhere  
✅ Better readability - clear intent and logic flow  

---

## YAGNI: You Aren't Gonna Need It

**Definition:** Don't add functionality until it's actually needed. Avoid speculative programming.

### The Problem

```csharp
// ❌ BAD - Over-engineering for features that may never be needed

public class UserService
{
    // Why implement this if we'll never need it?
    public void ExportUserToXml(User user) { }
    
    // Why implement this if we'll never need it?
    public void ExportUserToJson(User user) { }
    
    // Why implement this if we'll never need it?
    public void SyncUserWithLegacySystem(User user) { }
    
    // Why implement this if we'll never need it?
    public void CreateUserBackup(User user) { }
    
    // And five more methods "just in case"...
}

public class ConfigurationManager
{
    // Implemented a full plugin system "just in case"
    private List<IPlugin> _plugins = new();
    
    public void LoadPlugin(string path) { }
    public void UnloadPlugin(string pluginName) { }
    public void ExecutePlugin(string pluginName) { }
    
    // Still only used a tiny fraction
}
```

### The Solution

```csharp
// ✅ GOOD - Only implement what's needed now

public class UserService
{
    public void CreateUser(User user) { }
    public User GetUser(int id) { }
    public void UpdateUser(User user) { }
    public void DeleteUser(int id) { }
    
    // Add ExportToJson only when a requirement asks for it!
    // Add backup only when needed!
}

public class SimpleConfiguration
{
    private Dictionary<string, string> _settings;
    
    public void Set(string key, string value) => _settings[key] = value;
    public string Get(string key) => _settings[key];
    
    // No plugins, no complex features - just what we need
}
```

### YAGNI Examples

#### 1. **Don't Pre-Implement Features**

```csharp
// ❌ BAD
public interface IReportGenerator
{
    void GeneratePdf();
    void GenerateExcel();
    void GenerateWord();
    void GenerateJson();
    void GenerateCsv();
    void GenerateXml();
    void GenerateHtml();
}

// ✅ GOOD - Start simple
public interface IReportGenerator
{
    void GenerateExcel(); // This is what we need now
}

// When we need PDF later:
public interface IPdfReportGenerator
{
    void GeneratePdf();
}
```

#### 2. **Don't Over-Parameterize**

```csharp
// ❌ BAD - Too many optional parameters for features we might not need
public User CreateUser(
    string name,
    string email,
    string phone = null,
    string address = null,
    string preferences = null,
    string culturalBackground = null,
    string hobbies = null,
    string[] emergencyContacts = null,
    Dictionary<string, object> customData = null,
    bool enableTwoFactor = false,
    bool enableEmailNotifications = false,
    bool enablePushNotifications = false,
    bool enableSmsNotifications = false
)
{
    // Complex logic for many unused features
}

// ✅ GOOD - Minimal parameters, extend later as needed
public User CreateUser(string name, string email)
{
    return new User { Name = name, Email = email };
}

// Add features when actually needed
public void EnableTwoFactorAuthentication(User user) { }
```

#### 3. **Don't Create Abstract Base Classes Prematurely**

```csharp
// ❌ BAD - Abstraction for hypothetical subclasses
public abstract class BaseService
{
    public abstract void Validate();
    public abstract void Log();
    public abstract void HandleError();
    public abstract void SendNotification();
}

public class UserService : BaseService
{
    // Implementing methods that aren't needed
}

public class OrderService : BaseService
{
    // Implementing methods that aren't needed
}

// ✅ GOOD - Keep it simple until you need it
public class UserService
{
    public void ValidateUser(User user) { }
    public void CreateUser(User user) { }
}

public class OrderService
{
    public void ValidateOrder(Order order) { }
    public void CreateOrder(Order order) { }
}

// Refactor to a base class only when you have multiple similar implementations
```

### Benefits of YAGNI

✅ Simpler code - less to maintain  
✅ Faster development - focus on what matters  
✅ Fewer bugs - less code means fewer places for bugs  
✅ Better performance - no unnecessary features  
✅ Clearer intent - code shows what's actually used  

---

## DRY vs YAGNI: Balancing Act

| Scenario | DRY | YAGNI |
|----------|-----|-------|
| **Same logic in 3 places** | Extract immediately | Yes, extract once |
| **Might need in future** | Not DRY if speculative | Don't add it yet |
| **Could refactor to base class** | Check YAGNI first | Only if needed now |
| **Configuration repeated** | Extract immediately | Yes, one source of truth |
| **Complex "just in case" code** | Keep simple | Definitely don't add it |

---

## Common Mistakes

### ❌ Mistake 1: DRY Taken Too Far

```csharp
// Over-extracting - creating abstractions for every similarity
// Creates more complexity than it solves

// ✅ Better approach: Extract when complexity justifies it
```

### ❌ Mistake 2: Ignoring YAGNI

```csharp
// Building entire frameworks for features never used
// Wastes development time and creates maintenance burden
```

### ❌ Mistake 3: YAGNI as Excuse for Bad Code

```csharp
// Don't use YAGNI to avoid writing good code today
// Still write clean, maintainable code - just don't over-engineer
```

---

## Interview Tips

1. **Explain the philosophy** - Why these principles matter
2. **Trade-offs** - When to apply, when to be flexible
3. **Real examples** - Show code you've refactored
4. **Identify violations** - Practice spotting issues in code
5. **Balance** - Show you understand when to apply principles pragmatically

---

**Remember:** DRY and YAGNI work together - write clean code today without building for imaginary futures.
