# Access Modifiers in C#

Access modifiers control the visibility and accessibility of classes, methods, properties, and other members in C#. They are fundamental to encapsulation and a cornerstone of object-oriented programming.

---

## 1. Public Access Modifier

**Definition:** A `public` member is accessible from anywhere in your codebase and from external assemblies.

### When to Use
- For API endpoints you want others to use
- For library classes that are meant to be used by consumers
- For interface implementations
- For public-facing methods and properties

### Good Example

```csharp
// ✅ GOOD - Public interface for external consumption

public class UserService
{
    // Public method - accessible from anywhere
    public User GetUserById(int userId)
    {
        return new User { Id = userId, Name = "John Doe" };
    }
    
    // Public property - part of the public API
    public string ServiceName { get; set; } = "UserManagement";
    
    // Public method for user updates
    public void UpdateUser(User user)
    {
        // Update logic
    }
}

// Usage from another class
var userService = new UserService();
var user = userService.GetUserById(1); // Accessible everywhere
userService.ServiceName = "NewName"; // Accessible everywhere
```

### Benefits
- Clear API contracts
- Enables code reuse
- Documents intended usage

### Risks
- Can expose implementation details
- Makes it harder to refactor internal logic
- No protection for internal state

---

## 2. Private Access Modifier

**Definition:** A `private` member is only accessible within the same class. It is the most restrictive access level.

### When to Use
- For internal helper methods that shouldn't be exposed
- For internal state/fields
- For implementation details
- For preventing external modification of critical data

### Good Example

```csharp
// ✅ GOOD - Private implementation details

public class BankAccount
{
    // Private field - cannot be accessed from outside
    private decimal _balance;
    
    // Public property - controlled access to balance
    public decimal Balance
    {
        get { return _balance; }
    }
    
    // Public method - public interface
    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive");
        
        _balance += amount;
        LogTransaction("Deposit", amount); // Uses private helper
    }
    
    // Private helper method - not exposed to consumers
    private void LogTransaction(string type, decimal amount)
    {
        Console.WriteLine($"[{DateTime.Now}] {type}: ${amount}");
    }
    
    // Private method for internal validation
    private bool IsValidAmount(decimal amount)
    {
        return amount > 0 && amount < decimal.MaxValue;
    }
}

// Usage
var account = new BankAccount();
account.Deposit(100); // ✅ Allowed
// account._balance = -50; // ❌ NOT ALLOWED - Private field
// account.LogTransaction("Hack", 1000); // ❌ NOT ALLOWED - Private method
```

### Benefits
- Protects internal state
- Prevents misuse of internal helpers
- Allows safe refactoring of internal logic
- Enforces encapsulation

### Best Practices
- Make fields `private` by default
- Expose only what's necessary through `public` properties/methods
- Use private helper methods to keep complex logic organized

---

## 3. Protected Access Modifier

**Definition:** A `protected` member is accessible within the same class and in derived classes (subclasses). It's protected from external access but available to inheritance hierarchies.

### When to Use
- For methods/properties that subclasses need to override or use
- For base class behavior that should be customizable
- For helper methods needed by derived classes
- For template method patterns

### Good Example

```csharp
// ✅ GOOD - Protected for inheritance

public abstract class PaymentProcessor
{
    // Protected field - accessible to derived classes only
    protected decimal ProcessingFee = 0.02m;
    
    // Public method - main API
    public decimal ProcessPayment(decimal amount)
    {
        if (!ValidatePayment(amount)) // Uses protected validation
            throw new InvalidOperationException("Payment validation failed");
        
        decimal fee = CalculateFee(amount); // Uses protected calculation
        return amount + fee;
    }
    
    // Protected method - subclasses can override
    protected virtual decimal CalculateFee(decimal amount)
    {
        return amount * ProcessingFee;
    }
    
    // Protected method - subclasses must override
    protected abstract void LogPayment(decimal amount);
    
    // Protected helper - available to subclasses
    protected bool ValidatePayment(decimal amount)
    {
        return amount > 0;
    }
}

public class CreditCardProcessor : PaymentProcessor
{
    // Can override protected method
    protected override decimal CalculateFee(decimal amount)
    {
        // Credit cards have 2.5% fee instead of 2%
        return amount * 0.025m;
    }
    
    // Must implement abstract protected method
    protected override void LogPayment(decimal amount)
    {
        Console.WriteLine($"Credit card payment processed: ${amount}");
    }
}

// Usage
var processor = new CreditCardProcessor();
decimal total = processor.ProcessPayment(100); // Uses public method
// processor.CalculateFee(100); // ❌ NOT ALLOWED - Protected method
// processor.ValidatePayment(100); // ❌ NOT ALLOWED - Protected method
```

### Benefits
- Allows customization through inheritance
- Protects from external misuse while enabling internal extension
- Supports template method and strategy patterns
- Clear extension points for subclasses

---

## 4. Internal Access Modifier

**Definition:** An `internal` member is accessible only within the same assembly. It's used for code that should be hidden from external consumers.

### When to Use
- For classes/methods used only within your library
- For internal helpers that shouldn't be part of your public API
- For implementation details of public classes

### Good Example

```csharp
// In MyLibrary.dll

// ✅ GOOD - Internal implementation class

public class PublicUserService
{
    private readonly InternalUserRepository _repository;
    
    public PublicUserService()
    {
        _repository = new InternalUserRepository(); // Accessible in assembly
    }
    
    public User GetUser(int id)
    {
        return _repository.GetUser(id);
    }
}

// Internal - only visible within this assembly
internal class InternalUserRepository
{
    public User GetUser(int id)
    {
        // Database access logic
        return new User { Id = id };
    }
}

// In external code using MyLibrary.dll
var service = new PublicUserService();
var user = service.GetUser(1); // ✅ Allowed
// var repo = new InternalUserRepository(); // ❌ NOT ALLOWED - Internal class
```

### Benefits
- Hides implementation details
- Protects from external dependency on internal code
- Maintains clean public API surface

---

## 5. Protected Internal Access Modifier

**Definition:** A `protected internal` member is accessible within the same assembly OR from derived classes in other assemblies.

### When to Use
- For base class methods that should be available to subclasses across assemblies
- For internal helpers that subclasses might need
- Rarely used - usually protected or internal separately

### Good Example

```csharp
// In MyLibrary.dll

public abstract class BaseService
{
    // Accessible within assembly OR to derived classes anywhere
    protected internal virtual void LogActivity(string message)
    {
        Console.WriteLine($"[LOG] {message}");
    }
}

// In external assembly

public class ExtendedService : BaseService
{
    public void DoSomething()
    {
        LogActivity("Something happened"); // ✅ Allowed - derived class
    }
}
```

---

## 6. Private Protected Access Modifier (C# 7.2+)

**Definition:** A `private protected` member is accessible only within the same class and derived classes in the same assembly. More restrictive than `protected`.

### When to Use
- For methods derived classes need but should not be accessible outside the assembly
- Rare - most code uses `protected` or `private`

### Good Example

```csharp
public class BaseClass
{
    // Only derived classes in THIS assembly can access
    private protected void InternalHelper()
    {
        Console.WriteLine("Internal helper");
    }
}

public class DerivedClass : BaseClass
{
    public void CallHelper()
    {
        InternalHelper(); // ✅ Allowed - same assembly, derived class
    }
}

// In external assembly - NOT ALLOWED even for derived classes
public class ExternalDerived : BaseClass
{
    public void CallHelper()
    {
        // InternalHelper(); // ❌ NOT ALLOWED - different assembly
    }
}
```

---

## Access Modifiers Summary Table

| Modifier | Same Class | Same Assembly | Derived Class | External |
|----------|-----------|---------------|---------------|----------|
| **private** | ✅ | ❌ | ❌ | ❌ |
| **private protected** | ✅ | ✅ | ✅ (same assembly only) | ❌ |
| **protected** | ✅ | ✅ | ✅ | ✅ |
| **internal** | ✅ | ✅ | ❌ | ❌ |
| **protected internal** | ✅ | ✅ | ✅ | ✅ |
| **public** | ✅ | ✅ | ✅ | ✅ |

---

## Encapsulation Best Practices

### 1. **Start with Private, Increase Visibility as Needed**
```csharp
// ✅ GOOD - Start private, expose what's needed
public class Order
{
    private List<OrderItem> _items; // Private - internal state
    
    public IReadOnlyList<OrderItem> Items // Public read-only
    {
        get { return _items.AsReadOnly(); }
    }
    
    public void AddItem(OrderItem item) // Public method
    {
        _items.Add(item);
    }
}

// ❌ BAD - Everything public
public class Order
{
    public List<OrderItem> Items; // Too exposed!
}
```

### 2. **Protect Internal State with Properties**
```csharp
// ✅ GOOD - Control access through properties
public class Temperature
{
    private double _celsius;
    
    public double Celsius
    {
        get { return _celsius; }
        set
        {
            if (value < -273.15)
                throw new ArgumentException("Invalid temperature");
            _celsius = value;
        }
    }
}

// ❌ BAD - Direct field access
public class Temperature
{
    public double Celsius; // No validation possible
}
```

### 3. **Use Interfaces for Public Contracts**
```csharp
// ✅ GOOD - Interface defines what's public
public interface IUserService
{
    User GetUser(int id);
    void UpdateUser(User user);
}

public class UserService : IUserService
{
    public User GetUser(int id) // Implements interface
    {
        return FindUserInDatabase(id); // Private helper
    }
    
    private User FindUserInDatabase(int id) // Hidden implementation
    {
        // Database logic
        return new User();
    }
}
```

---

## Common Mistakes

### ❌ Making Everything Public
```csharp
// BAD - No encapsulation
public class Customer
{
    public string Name;
    public string Email;
    public DateTime CreatedDate;
    public void SendSpamEmail() { } // Should be private!
}
```

### ❌ Exposing Mutable Collections
```csharp
// BAD - External code can modify internal state
public class Team
{
    public List<Employee> Members; // Dangerous!
}

// GOOD - Return read-only collection
public class Team
{
    private List<Employee> _members;
    
    public IReadOnlyList<Employee> Members
    {
        get { return _members.AsReadOnly(); }
    }
}
```

### ❌ Protected When Private Would Suffice
```csharp
// BAD - More exposed than necessary
public class Engine
{
    protected void Ignite() { } // Not meant to be overridden
}

// GOOD - Explicitly private
public class Engine
{
    private void Ignite() { }
}
```

---

## Interview Tips

1. **Explain the WHY** - Access modifiers enable encapsulation and control
2. **Show trade-offs** - Public is convenient but risky; private is safe but restrictive
3. **Real examples** - Know how to protect sensitive state like passwords, account balances
4. **Inheritance patterns** - Understand when derived classes need protected access
5. **Assembly design** - Know when to use internal for multi-assembly projects

---

## Quick Reference

- **Use `public`** for your API - what others should use
- **Use `private`** by default - safest for internal implementation
- **Use `protected`** for base classes - lets subclasses extend behavior
- **Use `internal`** in libraries - hide implementation details
- **Avoid** `protected internal` and `private protected` unless you have a specific reason

---

**Remember:** Good encapsulation through proper access modifiers leads to maintainable, secure, and flexible code.
