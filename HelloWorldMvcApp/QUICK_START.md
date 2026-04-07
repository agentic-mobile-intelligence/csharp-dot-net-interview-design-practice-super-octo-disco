# 🚀 Quick Start Guide

Get the application running in 5 minutes!

## Prerequisites
- .NET 8.0 SDK ([Download](https://dotnet.microsoft.com/download))
- Terminal/Command Prompt
- Code editor (VS Code, Visual Studio, etc.)

## Quick Start Steps

### 1️⃣ Navigate to Project
```bash
cd HelloWorldMvcApp
```

### 2️⃣ Install Dependencies
```bash
dotnet restore
```

### 3️⃣ Run Application
```bash
dotnet run
```

### 4️⃣ Open Browser
```
https://localhost:7001
```

### 5️⃣ Explore
- Click through pages
- Watch visit counter increment
- Read about MVC architecture

## What You Should See

✅ Home page with greeting  
✅ About page with architecture info  
✅ Session-based visit counter  
✅ Professional styling  
✅ Working navigation  

## Project Files Overview

| File | Purpose |
|------|---------|
| `Program.cs` | Application startup & configuration |
| `Controllers/HomeController.cs` | Request handlers |
| `Models/` | Data structures (ViewModels) |
| `Views/` | HTML templates (Razor) |
| `Components/` | Blazor interactive components |
| `appsettings.json` | App configuration |

## Architecture at a Glance

```
Request → Controller → ViewModel → View → HTML
```

1. **Controller** - Handles request (HomeController)
2. **ViewModel** - Prepares data (HomeViewModel)
3. **View** - Renders HTML (Index.cshtml)
4. **Response** - Browser receives page

## Key Code Snippets

### Controller Example
```csharp
public IActionResult Index()
{
    var model = new HomeViewModel 
    { 
        Title = "Hello World",
        Message = "Welcome!"
    };
    return View(model);
}
```

### View Example
```html
@model HomeViewModel
<h1>@Model.Title</h1>
<p>@Model.Message</p>
```

## Common Commands

```bash
# Run application
dotnet run

# Build project
dotnet build

# Publish to production
dotnet publish -c Release

# Run tests (if added)
dotnet test

# Clean build artifacts
dotnet clean
```

## Troubleshooting

### Port Already in Use
Edit `Properties/launchSettings.json` and change port numbers

### .NET Version Mismatch
Check your .NET version:
```bash
dotnet --version
```
Should be 8.0 or later

### Dependencies Not Found
```bash
dotnet restore
dotnet clean
dotnet build
```

## Next Steps

1. ✅ Run the app
2. 📖 Read `PROJECT_GUIDE.md` for detailed explanation
3. 💡 Study the design patterns used
4. 🎯 Practice explaining the architecture
5. 🔧 Try adding new features:
   - New controller action
   - New ViewModel
   - New View page

## Common Interview Questions

**Q: Explain the MVC request flow**
A: Request hits routing → matches controller/action → controller creates model → returns view with model → view renders HTML

**Q: Why use ViewModels?**
A: Separates data transfer from domain models, follows SRP, easier to test

**Q: What is dependency injection?**
A: Injecting dependencies (like ILogger) into classes rather than creating them internally, enables loose coupling and testing

**Q: How does routing work?**
A: `{controller=Home}/{action=Index}/{id?}` pattern matches URL to controller actions

## Resources

- 📚 `PROJECT_GUIDE.md` - Comprehensive guide
- 📚 `../01-SOLID-Principles.md` - SOLID design principles
- 📚 `../03-Enterprise-Architecture-Patterns.md` - Patterns explained

## Get Help

Review files in this order:
1. This file (Quick Start)
2. PROJECT_GUIDE.md (detailed)
3. Parent directory guides for theory
4. Code comments in actual files

---

**You're ready! Run `dotnet run` and explore!** 🎯
