# SOLID Principles in C#

SOLID is an acronym for five design principles that make software designs more understandable, flexible, and maintainable.

---

## 1. Single Responsibility Principle (SRP)

**Definition:** A class should have only one reason to change, meaning it should only have one job or responsibility.

### Good Example

```csharp
// ✅ GOOD - Each class has a single responsibility

// Responsible only for user data
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Responsible only for saving users
public class UserRepository
{
    public void SaveUser(User user)
    {
        // Save to database
    }
    
    public User GetUser(int id)
    {
        // Retrieve from database
    }
}

// Responsible only for sending emails
public class EmailService
{
    public void SendWelcomeEmail(User user)
    {
        // Send email logic
    }
}
```

### Bad Example

```csharp
// ❌ BAD - User class has multiple responsibilities

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    
    // Database responsibility
    public void SaveToDatabase()
    {
        // DB logic
    }
    
    // Email responsibility
    public void SendWelcomeEmail()
    {
        // Email logic
    }
    
    // Validation responsibility
    public bool ValidateEmail()
    {
        // Validation logic
    }
}
```

### Benefits
- Easy to test (mock single responsibility)
- Better code organization
- Easier to modify without side effects

---

## 2. Open/Closed Principle (OCP)

**Definition:** Software entities should be open for extension but closed for modification.

### Good Example

```csharp
// ✅ GOOD - Open for extension, closed for modification

public abstract class PaymentProcessor
{
    public abstract void ProcessPayment(decimal amount);
}

public class CreditCardProcessor : PaymentProcessor
{
    public override void ProcessPayment(decimal amount)
    {
        Console.WriteLine($"Processing credit card payment: ${amount}");
    }
}

public class PayPalProcessor : PaymentProcessor
{
    public override void ProcessPayment(decimal amount)
    {
        Console.WriteLine($"Processing PayPal payment: ${amount}");
    }
}

// New payment type? Add new class, don't modify existing code
public class BitcoinProcessor : PaymentProcessor
{
    public override void ProcessPayment(decimal amount)
    {
        Console.WriteLine($"Processing Bitcoin payment: ${amount}");
    }
}

public class OrderProcessor
{
    public void ProcessOrder(PaymentProcessor processor, decimal amount)
    {
        processor.ProcessPayment(amount); // Works with any processor
    }
}
```

### Bad Example

```csharp
// ❌ BAD - Closed for extension, requires modification for new types

public class PaymentProcessor
{
    public void ProcessPayment(string paymentType, decimal amount)
    {
        if (paymentType == "CreditCard")
        {
            Console.WriteLine($"Processing credit card: ${amount}");
        }
        else if (paymentType == "PayPal")
        {
            Console.WriteLine($"Processing PayPal: ${amount}");
        }
        else if (paymentType == "Bitcoin")
        {
            // Had to modify this class!
            Console.WriteLine($"Processing Bitcoin: ${amount}");
        }
    }
}
```

### Benefits
- Easy to add new features without modifying existing code
- Reduces risk of breaking existing functionality
- Encourages abstraction and polymorphism

---

## 3. Liskov Substitution Principle (LSP)

**Definition:** Subtypes must be substitutable for their base types without altering the correctness of the program.

### Good Example

```csharp
// ✅ GOOD - Derived types can substitute base type

public abstract class Bird
{
    public abstract void Eat();
}

public class Sparrow : Bird
{
    public override void Eat()
    {
        Console.WriteLine("Sparrow is eating seeds");
    }
    
    public void Fly()
    {
        Console.WriteLine("Sparrow is flying");
    }
}

public class Penguin : Bird
{
    public override void Eat()
    {
        Console.WriteLine("Penguin is eating fish");
    }
    
    public void Swim()
    {
        Console.WriteLine("Penguin is swimming");
    }
}

// Penguin doesn't override Fly() - it's not a flying bird!
// We don't force it to implement Fly()

public class BirdFeeder
{
    public void FeedBird(Bird bird)
    {
        bird.Eat(); // Works correctly for all bird types
    }
}
```

### Bad Example

```csharp
// ❌ BAD - Penguin violates LSP by being forced to fly

public abstract class Bird
{
    public abstract void Eat();
    public abstract void Fly(); // All birds must fly!
}

public class Penguin : Bird
{
    public override void Eat()
    {
        Console.WriteLine("Penguin is eating fish");
    }
    
    public override void Fly()
    {
        throw new NotImplementedException("Penguins can't fly!");
        // This violates LSP - Penguin is not properly substitutable for Bird
    }
}
```

### Benefits
- Predictable behavior with inheritance
- Prevents runtime errors
- Makes polymorphism safe

---

## 4. Interface Segregation Principle (ISP)

**Definition:** Clients should not be forced to depend on interfaces they don't use.

### Good Example

```csharp
// ✅ GOOD - Segregated, focused interfaces

public interface IWorker
{
    void Work();
}

public interface IEater
{
    void Eat();
}

public interface IRobot : IWorker
{
    // Robots work but don't eat
}

public interface IHuman : IWorker, IEater
{
    // Humans work and eat
}

public class Robot : IRobot
{
    public void Work()
    {
        Console.WriteLine("Robot working");
    }
}

public class Human : IHuman
{
    public void Work()
    {
        Console.WriteLine("Human working");
    }
    
    public void Eat()
    {
        Console.WriteLine("Human eating");
    }
}
```

### Bad Example

```csharp
// ❌ BAD - Fat interface forcing unnecessary implementation

public interface IWorker
{
    void Work();
    void Eat();
    void Sleep();
    void Exercise();
}

public class Robot : IWorker
{
    public void Work()
    {
        Console.WriteLine("Robot working");
    }
    
    public void Eat()
    {
        throw new NotImplementedException(); // Forced to implement!
    }
    
    public void Sleep()
    {
        throw new NotImplementedException(); // Forced to implement!
    }
    
    public void Exercise()
    {
        throw new NotImplementedException(); // Forced to implement!
    }
}
```

### Benefits
- Classes only implement what they need
- Reduced coupling
- More flexible and testable code

---

## 5. Dependency Inversion Principle (DIP)

**Definition:** High-level modules should not depend on low-level modules. Both should depend on abstractions.

### Good Example

```csharp
// ✅ GOOD - Depend on abstraction, not concrete implementation

public interface IEmailService
{
    void SendEmail(string to, string message);
}

public class SmtpEmailService : IEmailService
{
    public void SendEmail(string to, string message)
    {
        Console.WriteLine($"Sending email via SMTP to {to}");
    }
}

public class UserNotificationService
{
    private readonly IEmailService _emailService;
    
    // Inject the abstraction
    public UserNotificationService(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public void NotifyUser(string email, string message)
    {
        _emailService.SendEmail(email, message);
    }
}

// Usage - can swap implementations easily
IEmailService emailService = new SmtpEmailService();
var notificationService = new UserNotificationService(emailService);
notificationService.NotifyUser("user@example.com", "Hello!");
```

### Bad Example

```csharp
// ❌ BAD - High-level module depends on low-level concrete class

public class SmtpEmailService
{
    public void SendEmail(string to, string message)
    {
        Console.WriteLine($"Sending email via SMTP to {to}");
    }
}

public class UserNotificationService
{
    private SmtpEmailService _emailService; // Direct dependency!
    
    public UserNotificationService()
    {
        _emailService = new SmtpEmailService(); // Hard to test, can't swap
    }
    
    public void NotifyUser(string email, string message)
    {
        _emailService.SendEmail(email, message);
    }
}
```

### Benefits
- Easy to test (inject mocks)
- Can swap implementations without changing code
- Reduces coupling between modules
- Supports dependency injection pattern

---

## SOLID Principles Summary Table

| Principle | Goal | Benefit |
|-----------|------|---------|
| **SRP** | One responsibility per class | Easy to test and maintain |
| **OCP** | Open for extension, closed for modification | Add features without breaking code |
| **LSP** | Substitutable subtypes | Safe polymorphism |
| **ISP** | Small, focused interfaces | Classes only implement what they need |
| **DIP** | Depend on abstractions | Flexible, testable, loosely coupled |

---

## Interview Tips

1. **Explain why, not just what** - Know the benefits of each principle
2. **Real-world examples** - Have project examples ready
3. **Trade-offs** - Understand when to apply and when to be pragmatic
4. **Common violations** - Be able to identify and fix violations
5. **SOLID + Other patterns** - Know how they work together

---

**Remember:** SOLID principles guide you to write better code, but they're not absolute rules. Use judgment and context.
