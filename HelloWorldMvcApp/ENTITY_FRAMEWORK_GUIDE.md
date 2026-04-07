# Entity Framework Core (EF Core) Complete Guide

## 📚 Overview

Entity Framework Core is an Object-Relational Mapping (ORM) library for .NET that allows you to work with databases using C# objects instead of SQL queries.

### Key Concepts

- **DbContext:** Manages database connections and queries
- **DbSet:** Represents a table in the database
- **Models:** C# classes representing database entities
- **Migrations:** Version control for database schema
- **LINQ:** Query syntax for accessing data

---

## 🗄️ Database Models

### 1. User Entity

```csharp
namespace HelloWorldMvcApp.Data.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

### 2. Order Entity

```csharp
namespace HelloWorldMvcApp.Data.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    // Foreign key navigation
    public User? User { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled
}
```

### 3. OrderItem Entity

```csharp
namespace HelloWorldMvcApp.Data.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Foreign keys
    public Order? Order { get; set; }
    public Product? Product { get; set; }
}
```

### 4. Product Entity

```csharp
namespace HelloWorldMvcApp.Data.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

---

## 💾 DbContext Setup

### Create ApplicationDbContext

```csharp
using Microsoft.EntityFrameworkCore;
using HelloWorldMvcApp.Data.Models;

namespace HelloWorldMvcApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSet properties represent tables
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============ CONFIGURE RELATIONSHIPS ============

        // User → Orders (One-to-Many)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order → OrderItems (One-to-Many)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Product → OrderItems (One-to-Many)
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============ CONFIGURE COLUMN PROPERTIES ============

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(10, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(10, 2);
    }
}
```

### Register in Program.cs

```csharp
// In Program.cs
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add repository pattern
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## 📋 Repository Pattern with EF Core

### IRepository Interface

```csharp
namespace HelloWorldMvcApp.Data.Repositories;

public interface IRepository<T> where T : class
{
    // Read operations
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

    // Write operations
    Task<T> AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task DeleteRangeAsync(IEnumerable<T> entities);

    // Utilities
    Task<int> CountAsync();
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
}
```

### Generic Repository Implementation

```csharp
namespace HelloWorldMvcApp.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;
    private readonly ILogger<Repository<T>> _logger;

    public Repository(ApplicationDbContext context, ILogger<Repository<T>> logger)
    {
        Context = context;
        DbSet = context.Set<T>();
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        try
        {
            return await DbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting {typeof(T).Name} by ID {id}");
            throw;
        }
    }

    public async Task<List<T>> GetAllAsync()
    {
        try
        {
            return await DbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting all {typeof(T).Name}");
            throw;
        }
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await DbSet.Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error finding {typeof(T).Name}");
            throw;
        }
    }

    public async Task<T> AddAsync(T entity)
    {
        try
        {
            await DbSet.AddAsync(entity);
            await Context.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding {typeof(T).Name}");
            throw;
        }
    }

    public async Task UpdateAsync(T entity)
    {
        try
        {
            DbSet.Update(entity);
            await Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating {typeof(T).Name}");
            throw;
        }
    }

    public async Task DeleteAsync(T entity)
    {
        try
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting {typeof(T).Name}");
            throw;
        }
    }

    public async Task<int> CountAsync()
    {
        return await DbSet.CountAsync();
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }
}
```

---

## 🔧 Unit of Work Pattern

### IUnitOfWork Interface

```csharp
namespace HelloWorldMvcApp.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Order> Orders { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Product> Products { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### Unit of Work Implementation

```csharp
namespace HelloWorldMvcApp.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public IRepository<User> Users { get; private set; }
    public IRepository<Order> Orders { get; private set; }
    public IRepository<OrderItem> OrderItems { get; private set; }
    public IRepository<Product> Products { get; private set; }

    public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;

        Users = new Repository<User>(context, logger);
        Orders = new Repository<Order>(context, logger);
        OrderItems = new Repository<OrderItem>(context, logger);
        Products = new Repository<Product>(context, logger);
    }

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes");
            throw;
        }
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.Database.CommitTransactionAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            await _context.Database.RollbackTransactionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

---

## 🚀 Common LINQ Queries

### Basic CRUD Operations

```csharp
// CREATE
var user = new User { Name = "John", Email = "john@example.com" };
await unitOfWork.Users.AddAsync(user);
await unitOfWork.SaveChangesAsync();

// READ
var user = await unitOfWork.Users.GetByIdAsync(1);
var allUsers = await unitOfWork.Users.GetAllAsync();
var activeUsers = await unitOfWork.Users.FindAsync(u => u.IsActive);

// UPDATE
user.Name = "Jane";
await unitOfWork.Users.UpdateAsync(user);

// DELETE
await unitOfWork.Users.DeleteAsync(user);
```

### Complex Queries

```csharp
// Include related data
var ordersWithItems = await _context.Orders
    .Include(o => o.User)
    .Include(o => o.Items)
    .ThenInclude(oi => oi.Product)
    .ToListAsync();

// Filtering
var recentOrders = await _context.Orders
    .Where(o => o.OrderDate > DateTime.UtcNow.AddDays(-30))
    .ToListAsync();

// Aggregation
var totalSales = await _context.Orders
    .Where(o => o.Status == OrderStatus.Completed)
    .SumAsync(o => o.TotalAmount);

// Grouping
var ordersByUser = await _context.Orders
    .GroupBy(o => o.UserId)
    .Select(g => new
    {
        UserId = g.Key,
        OrderCount = g.Count(),
        TotalAmount = g.Sum(o => o.TotalAmount)
    })
    .ToListAsync();

// Pagination
var page = 1;
var pageSize = 10;
var users = await _context.Users
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## 🔄 Migrations (Database Schema Control)

### Create Initial Migration

```bash
# From command line in project directory
dotnet ef migrations add InitialCreate

# This creates a migration file with:
# - Up() method: applies changes
# - Down() method: reverts changes
```

### Apply Migrations

```bash
# Apply to database
dotnet ef database update

# Update to specific migration
dotnet ef database update InitialCreate

# Revert to previous
dotnet ef database update PreviousMigration
```

### Generated Migration Example

```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Users table
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 255, nullable: false),
                Email = table.Column<string>(maxLength: 255, nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                IsActive = table.Column<bool>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            }
        );

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Users");
    }
}
```

---

## 🎯 Service Layer Example

### UserService with EF Core

```csharp
namespace HelloWorldMvcApp.Services;

public interface IUserService
{
    Task<User?> GetUserAsync(int id);
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(string name, string email);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(int id)
    {
        _logger.LogInformation($"Getting user {id}");
        return await _unitOfWork.Users.GetByIdAsync(id);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _unitOfWork.Users.GetAllAsync();
    }

    public async Task<User> CreateUserAsync(string name, string email)
    {
        // Business logic: validate
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Name and email are required");

        // Check if user already exists
        var existing = await _unitOfWork.Users
            .FindAsync(u => u.Email == email);

        if (existing.Any())
            throw new InvalidOperationException($"User with email {email} already exists");

        // Create user
        var user = new User { Name = name, Email = email };
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"User created: {user.Id}");
        return user;
    }

    public async Task UpdateUserAsync(User user)
    {
        await _unitOfWork.Users.UpdateAsync(user);
        _logger.LogInformation($"User updated: {user.Id}");
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user != null)
        {
            await _unitOfWork.Users.DeleteAsync(user);
            _logger.LogInformation($"User deleted: {id}");
        }
    }
}

// Register in Program.cs
builder.Services.AddScoped<IUserService, UserService>();
```

---

## 🧪 Testing with EF Core

### InMemory Database for Testing

```csharp
using Microsoft.EntityFrameworkCore;

public class UserServiceTests
{
    private ApplicationDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsUser()
    {
        // Arrange
        var context = GetInMemoryContext();
        var unitOfWork = new UnitOfWork(context, new Logger<UnitOfWork>(null));
        var service = new UserService(unitOfWork, new Logger<UserService>(null));

        // Act
        var user = await service.CreateUserAsync("John", "john@example.com");

        // Assert
        Assert.NotNull(user);
        Assert.Equal("John", user.Name);
        Assert.Equal("john@example.com", user.Email);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ThrowsException()
    {
        // Arrange
        var context = GetInMemoryContext();
        var unitOfWork = new UnitOfWork(context, new Logger<UnitOfWork>(null));
        var service = new UserService(unitOfWork, new Logger<UserService>(null));

        await service.CreateUserAsync("John", "john@example.com");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateUserAsync("Jane", "john@example.com")
        );
    }
}
```

---

## 📊 Performance Best Practices

### 1. Eager Loading (Include Related Data)

```csharp
// ✅ GOOD - Load related data
var orders = await _context.Orders
    .Include(o => o.User)
    .Include(o => o.Items)
    .ToListAsync();

// ❌ BAD - N+1 query problem
var orders = await _context.Orders.ToListAsync();
foreach (var order in orders)
{
    var user = await _context.Users.FindAsync(order.UserId); // Extra query!
}
```

### 2. Projection (Select Only Needed Columns)

```csharp
// ✅ GOOD - Only select needed columns
var userSummary = await _context.Users
    .Select(u => new { u.Id, u.Name, u.Email })
    .ToListAsync();

// ❌ BAD - Load entire entity
var users = await _context.Users.ToListAsync();
```

### 3. Filtering Before ToList()

```csharp
// ✅ GOOD - Filter in database
var activeUsers = await _context.Users
    .Where(u => u.IsActive)
    .ToListAsync();

// ❌ BAD - Load all then filter in memory
var activeUsers = (await _context.Users.ToListAsync())
    .Where(u => u.IsActive)
    .ToList();
```

### 4. Caching Frequently Accessed Data

```csharp
public class CachedUserService : IUserService
{
    private readonly IUserService _innerService;
    private readonly IMemoryCache _cache;

    public async Task<User?> GetUserAsync(int id)
    {
        const string cacheKey = $"user_{id}";

        if (_cache.TryGetValue(cacheKey, out User? user))
            return user;

        user = await _innerService.GetUserAsync(id);
        _cache.Set(cacheKey, user, TimeSpan.FromMinutes(10));
        return user;
    }
}
```

---

## 🎓 Interview Questions

### Q1: "Explain DbContext and DbSet"

**A:** DbContext manages database connections and tracks entity changes. DbSet represents a table. Example:

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; } // Represents Users table
}
```

### Q2: "What's the difference between Add and AddAsync?"

**A:** AddAsync is useful for generated IDs but both work the same for most cases. Use AddAsync when you need to generate IDs asynchronously.

### Q3: "How do you handle transactions?"

**A:**
```csharp
using (var transaction = await _context.Database.BeginTransactionAsync())
{
    try
    {
        // Perform operations
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
    }
}
```

### Q4: "What's the N+1 query problem?"

**A:** When you query parent entities then loop to query child entities separately, creating N additional queries. Solution: Use Include() for eager loading.

---

## 📚 Next Steps

1. Create migrations: `dotnet ef migrations add InitialCreate`
2. Apply to database: `dotnet ef database update`
3. Implement repositories for entities
4. Create services with business logic
5. Add to controllers and views
6. Test with in-memory database

---

**Entity Framework Core is essential for building scalable .NET applications!** 🚀
