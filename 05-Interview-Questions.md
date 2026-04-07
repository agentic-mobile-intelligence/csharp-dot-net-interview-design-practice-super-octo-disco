# C# Design Principles Interview Questions & Answers

Prepare for your interview with these common questions and comprehensive answers.

---

## SOLID Principles Questions

### Q1: Explain the Single Responsibility Principle

**Answer:**

The Single Responsibility Principle states that a class should have only one reason to change. In other words, it should have only one job or responsibility.

**Why it matters:**
- Makes classes easier to understand
- Easier to test (mock one responsibility)
- Changes to one responsibility don't affect others
- Reduces code coupling

**Example:**

```csharp
// ❌ WRONG - Multiple responsibilities
public class User
{
    public void CreateUser(string name) { }
    public void SaveToDatabase() { }
    public void SendEmail() { }
    public void ValidateEmail() { }
}

// ✅ RIGHT - One responsibility each
public class User { public string Name { get; set; } }
public class UserRepository { public void Save(User user) { } }
public class EmailService { public void Send(string email) { } }
public class EmailValidator { public bool Validate(string email) { } }
```

---

### Q2: What is the Open/Closed Principle and why is it important?

**Answer:**

The Open/Closed Principle states software should be **open for extension** but **closed for modification**. You should be able to add new functionality without changing existing code.

**Benefits:**
- Reduces risk of breaking existing functionality
- New features can be added without modifying core code
- Encourages use of abstractions and inheritance

**Example:**

```csharp
// ❌ WRONG - Must modify existing code for new payment types
public class PaymentProcessor
{
    public void ProcessPayment(string type, decimal amount)
    {
        if (type == "CreditCard") { /* ... */ }
        else if (type == "PayPal") { /* ... */ }
        else if (type == "Bitcoin") { /* Have to modify here! */ }
    }
}

// ✅ RIGHT - New payment types don't require changes
public abstract class PaymentProcessor
{
    public abstract void ProcessPayment(decimal amount);
}

public class CreditCardProcessor : PaymentProcessor
{
    public override void ProcessPayment(decimal amount) { }
}

public class BitcoinProcessor : PaymentProcessor
{
    public override void ProcessPayment(decimal amount) { }
}
```

---

### Q3: Explain Liskov Substitution Principle with an example

**Answer:**

The Liskov Substitution Principle states that derived classes must be substitutable for their base classes without breaking functionality.

**Key Point:** If class B is a subtype of class A, we should be able to replace A with B without disrupting the behavior of the program.

**Example:**

```csharp
// ❌ WRONG - Penguin violates LSP
public abstract class Bird
{
    public abstract void Fly();
}

public class Penguin : Bird
{
    public override void Fly()
    {
        throw new NotImplementedException("Penguins can't fly!");
    }
}

// ✅ RIGHT - Proper abstraction
public abstract class Bird
{
    public abstract void Move();
}

public class Sparrow : Bird
{
    public override void Move() => Console.WriteLine("Flying");
}

public class Penguin : Bird
{
    public override void Move() => Console.WriteLine("Swimming");
}
```

---

### Q4: What is Interface Segregation Principle?

**Answer:**

The Interface Segregation Principle states clients should not be forced to depend on interfaces they don't use. Create multiple specific interfaces instead of one general interface.

**Example:**

```csharp
// ❌ WRONG - Fat interface
public interface IWorker
{
    void Work();
    void Eat();
    void Sleep();
}

public class Robot : IWorker
{
    public void Work() { }
    public void Eat() { throw new NotImplementedException(); } // Forced!
    public void Sleep() { throw new NotImplementedException(); } // Forced!
}

// ✅ RIGHT - Segregated interfaces
public interface IWorker { void Work(); }
public interface IEater { void Eat(); }
public interface ISleeper { void Sleep(); }

public class Robot : IWorker
{
    public void Work() { }
}

public class Human : IWorker, IEater, ISleeper
{
    public void Work() { }
    public void Eat() { }
    public void Sleep() { }
}
```

---

### Q5: Explain Dependency Inversion Principle

**Answer:**

The Dependency Inversion Principle states high-level modules should not depend on low-level modules. Both should depend on abstractions.

**Why:**
- Reduces coupling
- Easier to test (inject mocks)
- Can swap implementations easily

**Example:**

```csharp
// ❌ WRONG - High-level depends on low-level
public class EmailService
{
    public void SendEmail() { }
}

public class OrderService
{
    private EmailService _emailService = new(); // Concrete dependency
}

// ✅ RIGHT - Both depend on abstraction
public interface IEmailService { void SendEmail(); }

public class EmailService : IEmailService { }

public class OrderService
{
    private readonly IEmailService _emailService;
    public OrderService(IEmailService emailService)
    {
        _emailService = emailService;
    }
}
```

---

## DRY & YAGNI Questions

### Q6: What is DRY and how would you refactor code to follow it?

**Answer:**

DRY means "Don't Repeat Yourself" - every piece of knowledge should appear in only one place.

**Example:**

```csharp
// ❌ WRONG - Email validation repeated
public class UserService
{
    public bool ValidateEmail(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }
}

public class AuthService
{
    public bool ValidateEmail(string email)
    {
        return email.Contains("@") && email.Contains("."); // Duplicated!
    }
}

// ✅ RIGHT - Single source of truth
public static class EmailValidator
{
    public static bool IsValid(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }
}

public class UserService
{
    public bool ValidateEmail(string email) => EmailValidator.IsValid(email);
}

public class AuthService
{
    public bool ValidateEmail(string email) => EmailValidator.IsValid(email);
}
```

---

### Q7: What is YAGNI and when should you apply it?

**Answer:**

YAGNI means "You Aren't Gonna Need It" - don't implement features until they're actually needed. Avoid speculative programming.

**Example:**

```csharp
// ❌ WRONG - Implementing features that may never be needed
public class UserService
{
    public void ExportToXml() { } // Not needed
    public void ExportToJson() { } // Not needed
    public void ExportToCsv() { } // Not needed
    public void ExportToExcel() { } // Not needed
    public void SyncWithLegacySystem() { } // Not needed
}

// ✅ RIGHT - Implement only what's needed
public class UserService
{
    public void CreateUser(User user) { }
    public User GetUser(int id) { }
    public void UpdateUser(User user) { }
    
    // Add ExportToJson only when a requirement actually asks for it
}
```

---

## Design Pattern Questions

### Q8: Explain the Repository Pattern

**Answer:**

The Repository Pattern provides an abstraction for data access, centralizing database query logic in one place.

**Benefits:**
- Easier to test (mock repository)
- Can change data source without affecting business logic
- Consistent data access patterns

**Example:**

```csharp
public interface IRepository<T>
{
    T GetById(int id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Delete(T entity);
}

public class UserRepository : IRepository<User>
{
    private readonly DbContext _context;
    
    public User GetById(int id) => _context.Users.Find(id);
    public IEnumerable<User> GetAll() => _context.Users.ToList();
    public void Add(User user) => _context.Users.Add(user);
    public void Delete(User user) => _context.Users.Remove(user);
}

// Usage - business logic doesn't care about database
public class UserService
{
    private readonly IRepository<User> _repository;
    
    public UserService(IRepository<User> repository)
    {
        _repository = repository;
    }
    
    public User GetUser(int id) => _repository.GetById(id);
}
```

---

### Q9: Explain Factory Pattern with a real-world example

**Answer:**

The Factory Pattern creates objects without specifying exact classes, using a factory interface.

**Benefits:**
- Decouples object creation from usage
- Easy to add new types
- Centralized creation logic

**Real-world example:**

```csharp
public interface IPaymentProcessor
{
    void ProcessPayment(decimal amount);
}

public class CreditCardProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount) => Console.WriteLine($"CC: {amount}");
}

public class PayPalProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount) => Console.WriteLine($"PP: {amount}");
}

public interface IPaymentFactory
{
    IPaymentProcessor Create(string type);
}

public class PaymentFactory : IPaymentFactory
{
    public IPaymentProcessor Create(string type)
    {
        return type.ToLower() switch
        {
            "creditcard" => new CreditCardProcessor(),
            "paypal" => new PayPalProcessor(),
            _ => throw new ArgumentException($"Unknown type: {type}")
        };
    }
}

// Usage
var factory = new PaymentFactory();
var processor = factory.Create("creditcard");
processor.ProcessPayment(100);
```

---

### Q10: When would you use Strategy vs Factory pattern?

**Answer:**

| Pattern | When to Use | Example |
|---------|-------------|---------|
| **Strategy** | Algorithm varies at runtime | Discount calculations (percentage, fixed, loyalty) |
| **Factory** | Object type varies at creation | Payment processor (CreditCard, PayPal, Bitcoin) |

**Strategy Example:**
```csharp
public interface IDiscountStrategy
{
    decimal Calculate(decimal amount);
}

public class PercentageDiscount : IDiscountStrategy
{
    public decimal Calculate(decimal amount) => amount * 0.10m;
}

var strategy = new PercentageDiscount();
decimal discountedPrice = strategy.Calculate(100); // Can swap strategies
```

**Factory Example:**
```csharp
public interface IPaymentProcessor { void Process(decimal amount); }

var processor = factory.Create("creditcard"); // Create once, use many times
processor.Process(100);
```

---

## Architectural Questions

### Q11: Design a user registration system

**Answer:**

Here's how I would approach this using design principles:

```csharp
// 1. Define domain models
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
}

// 2. Create abstractions
public interface IUserRepository { void Add(User user); }
public interface IEmailService { void SendConfirmation(User user); }
public interface IPasswordHasher { string Hash(string password); }
public interface IValidator { bool Validate(string email); }

// 3. Implement single responsibility
public class EmailValidator : IValidator
{
    public bool Validate(string email) => email.Contains("@");
}

public class UserRepository : IUserRepository
{
    private readonly DbContext _context;
    public void Add(User user) => _context.Users.Add(user);
}

// 4. Service layer with dependency injection
public class RegistrationService
{
    private readonly IUserRepository _repository;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _hasher;
    private readonly IValidator _validator;
    
    public RegistrationService(
        IUserRepository repository,
        IEmailService emailService,
        IPasswordHasher hasher,
        IValidator validator)
    {
        _repository = repository;
        _emailService = emailService;
        _hasher = hasher;
        _validator = validator;
    }
    
    public void Register(string email, string password)
    {
        if (!_validator.Validate(email))
            throw new InvalidOperationException("Invalid email");
            
        var user = new User
        {
            Email = email,
            PasswordHash = _hasher.Hash(password)
        };
        
        _repository.Add(user);
        _emailService.SendConfirmation(user);
    }
}
```

**Principles Applied:**
- ✅ SRP - Each class has one responsibility
- ✅ OCP - New validators don't require changes
- ✅ ISP - Small, focused interfaces
- ✅ DIP - Depend on abstractions
- ✅ DRY - Validation logic in one place

---

### Q12: How would you refactor a tightly coupled system?

**Answer:**

**Step 1: Identify the coupling**
```csharp
// ❌ Tight coupling
public class OrderService
{
    private SqlDatabase _db = new(); // Concrete dependency
    private EmailService _email = new();
    private PaymentProcessor _payment = new();
}
```

**Step 2: Create abstractions**
```csharp
public interface IDatabase { }
public interface IEmailService { }
public interface IPaymentProcessor { }
```

**Step 3: Inject dependencies**
```csharp
public class OrderService
{
    public OrderService(
        IDatabase db,
        IEmailService email,
        IPaymentProcessor payment)
    {
        // Store dependencies
    }
}
```

**Step 4: Configure DI container**
```csharp
services.AddScoped<IDatabase, SqlDatabase>();
services.AddScoped<IEmailService, SmtpEmailService>();
services.AddScoped<IPaymentProcessor, StripeProcessor>();
```

---

## Common Interview Traps

### Q13: "Should I always use interfaces?"

**Answer:**

No. Use interfaces when:
- You need multiple implementations
- You need to mock for testing
- You want to hide implementation details
- You're following an architectural pattern

Don't overuse them. Simple classes that don't need variations don't need interfaces.

---

### Q14: "Should I implement all SOLID principles?"

**Answer:**

SOLID are guidelines, not absolute rules. Use judgment:

✅ **Always:** SRP and DRY
⚠️ **Usually:** OCP and DIP
❓ **Sometimes:** LSP and ISP

Over-engineering wastes time. Balance pragmatism with good design.

---

### Q15: "How do you know when to refactor?"

**Answer:**

Refactor when:
- Code is hard to understand
- Changes require modifying multiple places
- Classes have too many responsibilities
- Dependencies are tightly coupled
- Tests are difficult to write

Don't refactor for no reason. "If it ain't broke, don't fix it" - but keep maintainability in mind.

---

### Q16: Where do you put business logic — database, API, or domain layer?

**Answer:**

Business logic belongs in the **domain layer**. The API layer handles HTTP routing, input validation, and request/response mapping. The database layer handles persistence, column types, and constraints. The domain layer owns the actual business rules — calculations, validations, and data transformations.

**Why the domain layer:**
- **Testable** without HTTP context or database connections
- **Portable** across data sources (CSV, Excel, PMS exports, APIs)
- **Single source of truth** for business rules
- **Decoupled** from infrastructure concerns

**Example — Hospitality/PMS system:**

```csharp
// Domain layer — business logic lives here
public class ReservationService
{
    private readonly IRoomRepository _roomRepository;

    public ReservationService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public decimal CalculateStayCost(Room room, DateTime checkIn, DateTime checkOut)
    {
        int nights = (checkOut - checkIn).Days;
        decimal cost = room.NightlyRate * nights;

        // Business rule: 7+ night discount
        if (nights >= 7) cost *= 0.90m;

        // Business rule: premium room surcharge
        if (room.Type == RoomType.Suite || room.Type == RoomType.Penthouse)
            cost *= 1.12m;

        return Math.Round(cost, 2);
    }
}

// API layer — orchestration only, no business rules
[HttpPost]
public IActionResult Create([FromBody] CreateReservationRequest request)
{
    var reservation = _reservationService.CreateReservation(
        request.RoomId, request.GuestName,
        request.CheckIn, request.CheckOut);

    return CreatedAtAction(nameof(GetById),
        new { id = reservation.ReservationId }, reservation);
}

// Infrastructure layer — data access, implements domain interfaces
public class CsvPmsDataSource : IPmsDataSource
{
    public IEnumerable<Room> ImportRooms()
    {
        // Parse CSV into the same domain models
        return File.ReadAllLines(_filePath)
            .Skip(1)
            .Select(line => MapToRoom(line));
    }
}
```

**Key point:** The API contract types (request/response DTOs) will look similar to domain models, but they serve different purposes. Domain models carry business meaning; API contracts handle serialization. Data from any source — Excel, CSV, PMS files, database — all flows through the same domain logic. Shared types, shared structures, shared data models between layers keep everything consistent.

---

## Practice Questions

Try answering these without looking at answers:

1. Describe a real situation where you violated SRP and how you fixed it
2. Give an example of using Dependency Injection in a project
3. Explain when you would use Observer vs Strategy pattern
4. Design a notification system using design patterns
5. How would you make a report generator support multiple formats?
6. Explain your approach to testing an OrderService
7. When would you use Repository pattern vs Entity Framework directly?
8. How do you handle complex object creation?
9. What makes a good interface design?
10. How do you refactor "god objects"?

---

## Interview Tips Summary

### ✅ DO:
- Listen carefully to requirements
- Ask clarifying questions
- Discuss trade-offs
- Draw diagrams
- Code what you explain
- Show your thinking process
- Admit what you don't know
- Use proper terminology

### ❌ DON'T:
- Jump to coding immediately
- Use patterns without reason
- Claim to never violate principles
- Over-engineer solutions
- Avoid admitting gaps
- Go off on tangents
- Copy-paste code without understanding

---

## Final Thoughts

- **Understand the "why"** not just the "what"
- **Be pragmatic** - principles serve the code, not the other way around
- **Practice** - implement these patterns in real projects
- **Read others' code** - learn from good examples
- **Stay humble** - good design is a journey, not a destination

Good luck with your interviews! 🎯
