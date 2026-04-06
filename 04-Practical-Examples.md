# Practical Examples & Real-World Applications

Real-world code examples showing design principles and patterns in action.

---

## Case Study 1: E-Commerce Order Processing System

### Problem Statement
Build an order processing system that:
- Handles multiple payment methods
- Applies different discount strategies
- Notifies multiple systems of order status
- Maintains clean separation of concerns

### Solution Using Patterns

```csharp
// ============ DOMAIN MODELS ============

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => Price * Quantity;
}

public enum OrderStatus
{
    Pending,
    Processing,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

// ============ INTERFACES & ABSTRACTIONS ============

// Discount Strategy (Strategy Pattern)
public interface IDiscountStrategy
{
    decimal CalculateDiscount(decimal subtotal, Customer customer);
}

public class NoDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(decimal subtotal, Customer customer) => 0;
}

public class PercentageDiscountStrategy : IDiscountStrategy
{
    private readonly decimal _percentage;
    
    public PercentageDiscountStrategy(decimal percentage)
    {
        _percentage = percentage;
    }
    
    public decimal CalculateDiscount(decimal subtotal, Customer customer)
    {
        return subtotal * (_percentage / 100);
    }
}

public class LoyaltyDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(decimal subtotal, Customer customer)
    {
        if (customer.TotalOrderValue > 5000)
            return subtotal * 0.15m;
        else if (customer.TotalOrderValue > 2000)
            return subtotal * 0.10m;
        else if (customer.TotalOrderValue > 500)
            return subtotal * 0.05m;
        
        return 0;
    }
}

// Payment Processing (Factory + Strategy Pattern)
public interface IPaymentProcessor
{
    PaymentResult ProcessPayment(Order order, PaymentDetails details);
    void RefundPayment(string transactionId);
}

public class CreditCardProcessor : IPaymentProcessor
{
    public PaymentResult ProcessPayment(Order order, PaymentDetails details)
    {
        // Process credit card payment
        return new PaymentResult
        {
            IsSuccessful = true,
            TransactionId = Guid.NewGuid().ToString(),
            Amount = order.Total
        };
    }
    
    public void RefundPayment(string transactionId)
    {
        // Process refund
    }
}

public class PayPalProcessor : IPaymentProcessor
{
    public PaymentResult ProcessPayment(Order order, PaymentDetails details)
    {
        // Process PayPal payment
        return new PaymentResult
        {
            IsSuccessful = true,
            TransactionId = Guid.NewGuid().ToString(),
            Amount = order.Total
        };
    }
    
    public void RefundPayment(string transactionId)
    {
        // Process refund via PayPal
    }
}

public interface IPaymentProcessorFactory
{
    IPaymentProcessor CreateProcessor(PaymentMethod method);
}

public class PaymentProcessorFactory : IPaymentProcessorFactory
{
    public IPaymentProcessor CreateProcessor(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.CreditCard => new CreditCardProcessor(),
            PaymentMethod.PayPal => new PayPalProcessor(),
            _ => throw new ArgumentException($"Unknown payment method: {method}")
        };
    }
}

// Notifications (Observer Pattern)
public interface IOrderObserver
{
    void OnOrderCreated(Order order);
    void OnPaymentProcessed(Order order);
    void OnOrderShipped(Order order);
}

public class EmailNotificationService : IOrderObserver
{
    private readonly IEmailService _emailService;
    
    public EmailNotificationService(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public void OnOrderCreated(Order order)
    {
        _emailService.SendEmail(
            order.Customer.Email,
            "Order Confirmation",
            $"Your order {order.OrderNumber} has been created."
        );
    }
    
    public void OnPaymentProcessed(Order order)
    {
        _emailService.SendEmail(
            order.Customer.Email,
            "Payment Confirmed",
            $"Payment of ${order.Total} has been received."
        );
    }
    
    public void OnOrderShipped(Order order)
    {
        _emailService.SendEmail(
            order.Customer.Email,
            "Order Shipped",
            $"Your order {order.OrderNumber} has been shipped."
        );
    }
}

public class InventoryService : IOrderObserver
{
    private readonly IInventoryRepository _repository;
    
    public InventoryService(IInventoryRepository repository)
    {
        _repository = repository;
    }
    
    public void OnOrderCreated(Order order)
    {
        // Reserve inventory
        foreach (var item in order.Items)
        {
            _repository.ReserveItem(item.ProductId, item.Quantity);
        }
    }
    
    public void OnPaymentProcessed(Order order)
    {
        // Confirm reservation
        foreach (var item in order.Items)
        {
            _repository.ConfirmReservation(item.ProductId, item.Quantity);
        }
    }
    
    public void OnOrderShipped(Order order)
    {
        // Mark items as shipped
    }
}

// Data Access (Repository Pattern)
public interface IOrderRepository
{
    void Add(Order order);
    Order GetById(int id);
    IEnumerable<Order> GetByCustomerId(int customerId);
    void Update(Order order);
}

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;
    
    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public void Add(Order order)
    {
        _context.Orders.Add(order);
        _context.SaveChanges();
    }
    
    public Order GetById(int id)
    {
        return _context.Orders
            .Include(o => o.Items)
            .FirstOrDefault(o => o.Id == id);
    }
    
    public IEnumerable<Order> GetByCustomerId(int customerId)
    {
        return _context.Orders
            .Where(o => o.CustomerId == customerId)
            .ToList();
    }
    
    public void Update(Order order)
    {
        _context.Orders.Update(order);
        _context.SaveChanges();
    }
}

// ============ BUSINESS LOGIC ============

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentProcessorFactory _paymentProcessorFactory;
    private readonly List<IOrderObserver> _observers = new();
    
    public OrderService(
        IOrderRepository orderRepository,
        IPaymentProcessorFactory paymentProcessorFactory)
    {
        _orderRepository = orderRepository;
        _paymentProcessorFactory = paymentProcessorFactory;
    }
    
    // Observer Pattern - Subscribe
    public void Subscribe(IOrderObserver observer)
    {
        _observers.Add(observer);
    }
    
    // Create order with discount strategy
    public void CreateOrder(
        List<OrderItem> items,
        Customer customer,
        IDiscountStrategy discountStrategy)
    {
        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            Items = items,
            Subtotal = items.Sum(i => i.LineTotal),
            CustomerId = customer.Id,
            Status = OrderStatus.Pending
        };
        
        // Calculate discount using strategy
        order.Discount = discountStrategy.CalculateDiscount(order.Subtotal, customer);
        order.Total = order.Subtotal - order.Discount;
        
        _orderRepository.Add(order);
        
        // Notify all observers
        foreach (var observer in _observers)
        {
            observer.OnOrderCreated(order);
        }
    }
    
    // Process payment using factory
    public PaymentResult ProcessPayment(
        Order order,
        PaymentMethod paymentMethod,
        PaymentDetails details)
    {
        var processor = _paymentProcessorFactory.CreateProcessor(paymentMethod);
        var result = processor.ProcessPayment(order, details);
        
        if (result.IsSuccessful)
        {
            order.Status = OrderStatus.Confirmed;
            _orderRepository.Update(order);
            
            // Notify observers of payment
            foreach (var observer in _observers)
            {
                observer.OnPaymentProcessed(order);
            }
        }
        
        return result;
    }
    
    private string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}";
    }
}

// ============ DEPENDENCY INJECTION SETUP ============

public static class ServiceConfiguration
{
    public static void ConfigureOrderServices(IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        
        // Payment Processing
        services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();
        
        // Services
        services.AddScoped<OrderService>();
        
        // Observers
        services.AddScoped<EmailNotificationService>();
        services.AddScoped<InventoryService>();
    }
}

// ============ USAGE EXAMPLE ============

public class OrderProcessingExample
{
    public static void Main()
    {
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureOrderServices(services);
        var provider = services.BuildServiceProvider();
        
        var orderService = provider.GetRequiredService<OrderService>();
        var emailService = provider.GetRequiredService<EmailNotificationService>();
        var inventoryService = provider.GetRequiredService<InventoryService>();
        
        // Subscribe to order events
        orderService.Subscribe(emailService);
        orderService.Subscribe(inventoryService);
        
        // Create order with loyalty discount
        var items = new List<OrderItem>
        {
            new OrderItem { ProductId = 1, ProductName = "Laptop", Price = 999, Quantity = 1 },
            new OrderItem { ProductId = 2, ProductName = "Mouse", Price = 29, Quantity = 2 }
        };
        
        var customer = new Customer { Id = 1, Email = "john@example.com", TotalOrderValue = 3500 };
        var discountStrategy = new LoyaltyDiscountStrategy();
        
        orderService.CreateOrder(items, customer, discountStrategy);
        
        // Process payment
        var order = new Order { Total = 1057m };
        var paymentResult = orderService.ProcessPayment(
            order,
            PaymentMethod.CreditCard,
            new PaymentDetails { }
        );
        
        Console.WriteLine($"Payment successful: {paymentResult.IsSuccessful}");
    }
}
```

### Principles Applied
- **SRP:** Each class has one responsibility
- **OCP:** New payment methods/discounts don't require existing code changes
- **DIP:** All classes depend on interfaces
- **Strategy Pattern:** Discount calculation varies by strategy
- **Factory Pattern:** Payment processor creation is centralized
- **Observer Pattern:** Multiple systems notified of order events
- **Repository Pattern:** Data access is abstracted
- **DRY:** Common logic is centralized
- **YAGNI:** Only necessary functionality is implemented

---

## Case Study 2: Logging and Monitoring System

### Problem Statement
Create a flexible logging system that:
- Supports multiple output destinations
- Can be extended with new logging methods
- Doesn't change when new features are added
- Maintains single responsibility per class

### Solution

```csharp
// ============ ABSTRACTION ============

// Strategy Pattern for different logging destinations
public interface ILogger
{
    void Log(LogLevel level, string message);
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

// ============ IMPLEMENTATIONS ============

public class ConsoleLogger : ILogger
{
    public void Log(LogLevel level, string message)
    {
        Console.ForegroundColor = level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
        
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
        Console.ResetColor();
    }
}

public class FileLogger : ILogger
{
    private readonly string _filePath;
    
    public FileLogger(string filePath)
    {
        _filePath = filePath;
    }
    
    public void Log(LogLevel level, string message)
    {
        var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        File.AppendAllText(_filePath, logEntry + Environment.NewLine);
    }
}

// ============ COMPOSITE PATTERN ============

public class CompositeLogger : ILogger
{
    private readonly List<ILogger> _loggers = new();
    
    public void AddLogger(ILogger logger)
    {
        _loggers.Add(logger);
    }
    
    public void Log(LogLevel level, string message)
    {
        foreach (var logger in _loggers)
        {
            logger.Log(level, message);
        }
    }
}

// ============ DECORATOR PATTERN ============

public class TimestampDecorator : ILogger
{
    private readonly ILogger _innerLogger;
    
    public TimestampDecorator(ILogger innerLogger)
    {
        _innerLogger = innerLogger;
    }
    
    public void Log(LogLevel level, string message)
    {
        var enhancedMessage = $"[{DateTime.UtcNow:O}] {message}";
        _innerLogger.Log(level, enhancedMessage);
    }
}

// ============ USAGE ============

var compositeLogger = new CompositeLogger();
compositeLogger.AddLogger(new ConsoleLogger());
compositeLogger.AddLogger(new FileLogger("app.log"));

// Add timestamp enhancement
ILogger logger = new TimestampDecorator(compositeLogger);

logger.Log(LogLevel.Info, "Application started");
logger.Log(LogLevel.Warning, "Low memory warning");
logger.Log(LogLevel.Error, "Database connection failed");
```

### Key Points
✅ **OCP:** New logging destinations don't require code changes  
✅ **SRP:** Each logger handles one output method  
✅ **Composite Pattern:** Log to multiple destinations at once  
✅ **Decorator Pattern:** Add features without modifying original classes  
✅ **DRY:** Common logging logic is centralized  

---

## Common Anti-Patterns to Avoid

### ❌ Anti-Pattern 1: God Class

```csharp
// BAD - One class doing everything
public class UserManager
{
    public void CreateUser() { }
    public void ValidateUser() { }
    public void SaveUserToDatabase() { }
    public void SendWelcomeEmail() { }
    public void GenerateReport() { }
    public void UpdateUserPermissions() { }
    public void AuthenticateUser() { }
    // And 10 more methods...
}

// ✅ GOOD - Separate responsibilities
public class UserValidator { }
public class UserRepository { }
public class EmailService { }
public class ReportGenerator { }
public class AuthenticationService { }
```

### ❌ Anti-Pattern 2: Tight Coupling

```csharp
// BAD
public class OrderProcessor
{
    public void ProcessOrder(Order order)
    {
        var paymentProcessor = new CreditCardProcessor(); // Hard-coded
        paymentProcessor.ProcessPayment(order);
    }
}

// ✅ GOOD
public class OrderProcessor
{
    private readonly IPaymentProcessor _paymentProcessor;
    
    public OrderProcessor(IPaymentProcessor paymentProcessor)
    {
        _paymentProcessor = paymentProcessor;
    }
    
    public void ProcessOrder(Order order)
    {
        _paymentProcessor.ProcessPayment(order);
    }
}
```

### ❌ Anti-Pattern 3: Leaky Abstractions

```csharp
// BAD - Implementation details leak out
public interface IUserRepository
{
    DataTable GetUsers(); // DataTable is implementation detail
    void SaveToSqlDatabase(User user); // Mentions specific DB
}

// ✅ GOOD - Clean abstraction
public interface IUserRepository
{
    IEnumerable<User> GetUsers();
    void Save(User user);
}
```

---

## Interview Preparation Checklist

### Before Your Interview

- [ ] Understand SOLID principles deeply
- [ ] Know DRY and YAGNI philosophy
- [ ] Memorize 3-5 key patterns
- [ ] Prepare code examples from your work
- [ ] Practice identifying violations
- [ ] Understand trade-offs of each pattern
- [ ] Know when NOT to use a pattern
- [ ] Be ready to discuss refactoring approaches

### During Your Interview

✅ Listen carefully to the problem  
✅ Ask clarifying questions  
✅ Think out loud about design decisions  
✅ Discuss trade-offs and alternatives  
✅ Use proper terminology  
✅ Draw diagrams if helpful  
✅ Code examples should be clean and correct  
✅ Admit when you don't know something  

---

## Key Takeaways for Interviews

1. **Principles over Patterns** - Understand why patterns exist
2. **Real-world Context** - Show you've applied these in practice
3. **Pragmatism** - Know when to break rules
4. **Communication** - Explain your thinking clearly
5. **Continuous Learning** - Show growth mindset

---

**Remember:** Good design is about making code maintainable, testable, and scalable. Always think about the problem you're solving, not just the pattern you're applying.
