# Enterprise Architecture Patterns in C#

Essential architectural patterns used in enterprise applications for scalability, maintainability, and separation of concerns.

---

## 1. MVC (Model-View-Controller)

**Definition:** Separates application into three interconnected components: Model (data), View (UI), and Controller (logic).

### Architecture

```
┌─────────────────┐
│      User       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Controller    │◄─── Handles user input
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌───────┐  ┌──────┐
│ Model │  │ View │
└───────┘  └──────┘
    │         │
    └────┬────┘
         ▼
    ┌─────────┐
    │Database │
    └─────────┘
```

### Implementation

```csharp
// Model - Data and business logic
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Controller - Handles requests and responses
public class ProductController : Controller
{
    private readonly IProductService _productService;
    
    public ProductController(IProductService productService)
    {
        _productService = productService;
    }
    
    public IActionResult Index()
    {
        var products = _productService.GetAllProducts();
        return View(products); // Render View with Model
    }
    
    public IActionResult Create(Product product)
    {
        _productService.CreateProduct(product);
        return RedirectToAction("Index");
    }
}

// View - HTML/User Interface (Razor)
// Views/Product/Index.cshtml
/*
@model List<Product>

<h1>Products</h1>
<table>
    @foreach(var product in Model)
    {
        <tr>
            <td>@product.Name</td>
            <td>@product.Price</td>
        </tr>
    }
</table>
*/
```

### Advantages
✅ Clear separation of concerns  
✅ Easy to test (especially with dependency injection)  
✅ Multiple views can use same model  
✅ Familiar pattern for many developers  

### Disadvantages
❌ Can lead to "fat controllers"  
❌ Tight coupling between View and Model  
❌ Testing views is difficult  

---

## 2. MVVM (Model-View-ViewModel)

**Definition:** Separates application into Model (data), View (UI), and ViewModel (state and behavior for the View).

### Architecture

```
┌──────────────┐
│     View     │
│  (UI Layer)  │
└──────┬───────┘
       │ Binds to
       ▼
┌──────────────────┐
│   ViewModel      │
│ (Presentation)   │
└──────┬───────────┘
       │ Uses
       ▼
┌──────────────┐
│    Model     │
│  (Business)  │
└──────────────┘
```

### Implementation

```csharp
// Model
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// ViewModel - Data and logic for View
public class ProductViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Product> _products;
    
    public ObservableCollection<Product> Products
    {
        get => _products;
        set 
        { 
            _products = value;
            OnPropertyChanged(nameof(Products));
        }
    }
    
    private Product _selectedProduct;
    public Product SelectedProduct
    {
        get => _selectedProduct;
        set 
        { 
            _selectedProduct = value;
            OnPropertyChanged(nameof(SelectedProduct));
        }
    }
    
    public ICommand CreateProductCommand { get; }
    public ICommand DeleteProductCommand { get; }
    
    private readonly IProductService _productService;
    
    public ProductViewModel(IProductService productService)
    {
        _productService = productService;
        CreateProductCommand = new RelayCommand(CreateProduct);
        DeleteProductCommand = new RelayCommand(DeleteProduct);
        LoadProducts();
    }
    
    private void LoadProducts()
    {
        Products = new ObservableCollection<Product>(_productService.GetAllProducts());
    }
    
    private void CreateProduct()
    {
        if (SelectedProduct != null)
        {
            _productService.CreateProduct(SelectedProduct);
            LoadProducts();
        }
    }
    
    private void DeleteProduct()
    {
        if (SelectedProduct != null)
        {
            _productService.DeleteProduct(SelectedProduct.Id);
            LoadProducts();
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// View (WPF/XAML)
/*
<Window>
    <Grid>
        <ItemsControl ItemsSource="{Binding Products}">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Name}"/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        <Button Command="{Binding CreateProductCommand}">Create</Button>
        <Button Command="{Binding DeleteProductCommand}">Delete</Button>
    </Grid>
</Window>
*/
```

### Advantages
✅ Clean separation between View and logic  
✅ Excellent testability (ViewModel has no UI dependencies)  
✅ Two-way data binding reduces boilerplate  
✅ Reusable ViewModels  

### Disadvantages
❌ Learning curve for data binding  
❌ Requires frameworks like WPF or frameworks  
❌ Can become complex with multiple ViewModels  

---

## 3. Repository Pattern

**Definition:** Provides an abstraction for data access, centralizing data retrieval logic.

### Implementation

```csharp
// Abstraction
public interface IRepository<T> where T : class
{
    T GetById(int id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
    void SaveChanges();
}

// Concrete Implementation
public class UserRepository : IRepository<User>
{
    private readonly ApplicationDbContext _context;
    
    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public User GetById(int id)
    {
        return _context.Users.FirstOrDefault(u => u.Id == id);
    }
    
    public IEnumerable<User> GetAll()
    {
        return _context.Users.ToList();
    }
    
    public void Add(User user)
    {
        _context.Users.Add(user);
    }
    
    public void Update(User user)
    {
        _context.Users.Update(user);
    }
    
    public void Delete(User user)
    {
        _context.Users.Remove(user);
    }
    
    public void SaveChanges()
    {
        _context.SaveChanges();
    }
}

// Usage
public class UserService
{
    private readonly IRepository<User> _userRepository;
    
    public UserService(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }
    
    public void CreateUser(string name, string email)
    {
        var user = new User { Name = name, Email = email };
        _userRepository.Add(user);
        _userRepository.SaveChanges();
    }
    
    public User GetUser(int id)
    {
        return _userRepository.GetById(id);
    }
}
```

### Advantages
✅ Centralizes data access logic  
✅ Easy to test (swap repository with mock)  
✅ Database agnostic (can switch providers)  
✅ Consistent data access patterns  

---

## 4. Dependency Injection (DI)

**Definition:** Provides dependencies to a class rather than having the class create them.

### Without DI (Tightly Coupled)

```csharp
// ❌ BAD - Hard to test, tightly coupled
public class OrderService
{
    private readonly DatabaseConnection _db;
    private readonly EmailService _emailService;
    
    public OrderService()
    {
        _db = new DatabaseConnection(); // Creates its own dependencies
        _emailService = new EmailService();
    }
}
```

### With DI (Loosely Coupled)

```csharp
// ✅ GOOD - Easy to test, loosely coupled
public interface IDatabaseConnection { }
public interface IEmailService { }

public class OrderService
{
    private readonly IDatabaseConnection _db;
    private readonly IEmailService _emailService;
    
    // Dependencies are injected
    public OrderService(IDatabaseConnection db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }
}

// In Startup/Configuration
services.AddScoped<IDatabaseConnection, DatabaseConnection>();
services.AddScoped<IEmailService, SmtpEmailService>();
services.AddScoped<OrderService>();
```

### Benefits
✅ Easy to test with mocks  
✅ Flexible (swap implementations)  
✅ Follows SOLID principles  
✅ Automatic dependency resolution  

---

## 5. Factory Pattern

**Definition:** Creates objects without specifying exact classes, using a factory interface.

### Implementation

```csharp
// Product Abstraction
public interface IPaymentProcessor
{
    void ProcessPayment(decimal amount);
}

// Concrete Products
public class CreditCardProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount)
    {
        Console.WriteLine($"Processing credit card: ${amount}");
    }
}

public class PayPalProcessor : IPaymentProcessor
{
    public void ProcessPayment(decimal amount)
    {
        Console.WriteLine($"Processing PayPal: ${amount}");
    }
}

// Factory
public interface IPaymentProcessorFactory
{
    IPaymentProcessor CreateProcessor(string type);
}

public class PaymentProcessorFactory : IPaymentProcessorFactory
{
    public IPaymentProcessor CreateProcessor(string type)
    {
        return type.ToLower() switch
        {
            "creditcard" => new CreditCardProcessor(),
            "paypal" => new PayPalProcessor(),
            _ => throw new ArgumentException($"Unknown processor type: {type}")
        };
    }
}

// Usage
var factory = new PaymentProcessorFactory();
var processor = factory.CreateProcessor("creditcard");
processor.ProcessPayment(100);
```

### Advantages
✅ Decouples creation logic from usage  
✅ Easy to add new types  
✅ Centralized object creation  
✅ Follows Open/Closed Principle  

---

## 6. Decorator Pattern

**Definition:** Adds behavior to objects dynamically without modifying their structure.

### Implementation

```csharp
// Component
public interface ICoffee
{
    string GetDescription();
    decimal GetCost();
}

public class SimpleCoffee : ICoffee
{
    public string GetDescription() => "Coffee";
    public decimal GetCost() => 2.00m;
}

// Decorators
public abstract class CoffeeDecorator : ICoffee
{
    protected ICoffee _coffee;
    
    public CoffeeDecorator(ICoffee coffee)
    {
        _coffee = coffee;
    }
    
    public virtual string GetDescription() => _coffee.GetDescription();
    public virtual decimal GetCost() => _coffee.GetCost();
}

public class MilkDecorator : CoffeeDecorator
{
    public MilkDecorator(ICoffee coffee) : base(coffee) { }
    
    public override string GetDescription() => $"{_coffee.GetDescription()}, Milk";
    public override decimal GetCost() => _coffee.GetCost() + 0.50m;
}

public class VanillaDecorator : CoffeeDecorator
{
    public VanillaDecorator(ICoffee coffee) : base(coffee) { }
    
    public override string GetDescription() => $"{_coffee.GetDescription()}, Vanilla";
    public override decimal GetCost() => _coffee.GetCost() + 0.75m;
}

// Usage
ICoffee coffee = new SimpleCoffee();
coffee = new MilkDecorator(coffee);
coffee = new VanillaDecorator(coffee);

Console.WriteLine(coffee.GetDescription()); // Coffee, Milk, Vanilla
Console.WriteLine(coffee.GetCost());        // 3.25
```

### Advantages
✅ Add behavior without modifying original class  
✅ Flexible combinations  
✅ Follows Single Responsibility Principle  
✅ Better than inheritance for adding behavior  

---

## 7. Strategy Pattern

**Definition:** Defines a family of algorithms and makes them interchangeable.

### Implementation

```csharp
// Strategy Interface
public interface IDiscountStrategy
{
    decimal CalculateDiscount(decimal originalPrice);
}

// Concrete Strategies
public class NoDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(decimal originalPrice) => 0;
}

public class PercentageDiscountStrategy : IDiscountStrategy
{
    private readonly decimal _percentage;
    
    public PercentageDiscountStrategy(decimal percentage)
    {
        _percentage = percentage;
    }
    
    public decimal CalculateDiscount(decimal originalPrice)
    {
        return originalPrice * (_percentage / 100);
    }
}

public class FixedAmountDiscountStrategy : IDiscountStrategy
{
    private readonly decimal _amount;
    
    public FixedAmountDiscountStrategy(decimal amount)
    {
        _amount = amount;
    }
    
    public decimal CalculateDiscount(decimal originalPrice)
    {
        return _amount;
    }
}

// Context
public class ShoppingCart
{
    private IDiscountStrategy _discountStrategy;
    private decimal _subtotal;
    
    public ShoppingCart(decimal subtotal, IDiscountStrategy strategy)
    {
        _subtotal = subtotal;
        _discountStrategy = strategy;
    }
    
    public void SetDiscountStrategy(IDiscountStrategy strategy)
    {
        _discountStrategy = strategy;
    }
    
    public decimal GetTotal()
    {
        decimal discount = _discountStrategy.CalculateDiscount(_subtotal);
        return _subtotal - discount;
    }
}

// Usage
var cart = new ShoppingCart(100, new NoDiscountStrategy());
Console.WriteLine(cart.GetTotal()); // 100

cart.SetDiscountStrategy(new PercentageDiscountStrategy(10));
Console.WriteLine(cart.GetTotal()); // 90

cart.SetDiscountStrategy(new FixedAmountDiscountStrategy(15));
Console.WriteLine(cart.GetTotal()); // 85
```

### Advantages
✅ Flexible algorithm selection at runtime  
✅ Easy to add new strategies  
✅ Follows Open/Closed Principle  
✅ Reduces conditional logic  

---

## 8. Observer Pattern

**Definition:** Defines a one-to-many relationship where observers are notified of state changes.

### Implementation

```csharp
// Observable
public interface INotifier
{
    void Subscribe(IObserver observer);
    void Unsubscribe(IObserver observer);
    void NotifyObservers();
}

public class StockPrice : INotifier
{
    private decimal _price;
    private List<IObserver> _observers = new();
    
    public decimal Price
    {
        get => _price;
        set
        {
            if (_price != value)
            {
                _price = value;
                NotifyObservers();
            }
        }
    }
    
    public void Subscribe(IObserver observer) => _observers.Add(observer);
    public void Unsubscribe(IObserver observer) => _observers.Remove(observer);
    
    public void NotifyObservers()
    {
        foreach (var observer in _observers)
        {
            observer.Update(this);
        }
    }
}

// Observer
public interface IObserver
{
    void Update(StockPrice stockPrice);
}

public class StockPortfolio : IObserver
{
    public void Update(StockPrice stockPrice)
    {
        Console.WriteLine($"Portfolio updated: Stock price is now ${stockPrice.Price}");
    }
}

public class TradingBot : IObserver
{
    public void Update(StockPrice stockPrice)
    {
        Console.WriteLine($"Trading bot notified: Stock price is now ${stockPrice.Price}");
    }
}

// Usage
var stock = new StockPrice();
var portfolio = new StockPortfolio();
var bot = new TradingBot();

stock.Subscribe(portfolio);
stock.Subscribe(bot);

stock.Price = 150.00m; // Both observers are notified
stock.Price = 155.50m; // Both observers are notified again
```

### Advantages
✅ Loose coupling between components  
✅ Dynamic subscription/unsubscription  
✅ Supports event-driven architecture  
✅ Easy to extend with new observers  

---

## Pattern Selection Guide

| Scenario | Pattern | Why |
|----------|---------|-----|
| **Separate data/business/presentation logic** | MVC/MVVM | Clear separation |
| **Centralize data access** | Repository | Single source of truth |
| **Manage object creation** | Factory | Decouple creation |
| **Add behavior dynamically** | Decorator | Without modification |
| **Choose algorithms at runtime** | Strategy | Flexibility |
| **Notify multiple objects of changes** | Observer | Event-driven |
| **Inject dependencies** | DI | Loose coupling |

---

## Interview Tips

1. **Understand the problem each pattern solves** - Know the "why"
2. **Trade-offs** - When to use and when to avoid
3. **Implementation details** - Know how to code them
4. **Real-world examples** - Have project examples
5. **Patterns together** - Understand how patterns work with each other

---

**Remember:** Patterns are tools to solve specific problems, not requirements for every project.
