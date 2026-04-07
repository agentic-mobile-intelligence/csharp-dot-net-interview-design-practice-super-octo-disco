using Microsoft.AspNetCore.Mvc;
using HelloWorldMvcApp.Models;

namespace HelloWorldMvcApp.Controllers;

/// <summary>
/// Home Controller - Handles home page requests
/// Demonstrates Single Responsibility Principle (SRP)
/// This controller only handles navigation/home page logic
/// </summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET: Home/Index
    /// Returns the home page
    /// </summary>
    public IActionResult Index()
    {
        _logger.LogInformation("Home/Index accessed");

        var viewModel = new HomeViewModel
        {
            Title = "Hello World - MVC Application",
            Message = "Welcome to ASP.NET Core MVC with Blazor!",
            VisitCount = GetOrIncrementVisitCount()
        };

        return View(viewModel);
    }

    /// <summary>
    /// GET: Home/About
    /// Returns the about page
    /// </summary>
    public IActionResult About()
    {
        _logger.LogInformation("Home/About accessed");

        var viewModel = new AboutViewModel
        {
            Title = "About This Application",
            Description = "This is a simple .NET Core MVC application demonstrating design principles for interview preparation.",
            Author = "Interview Candidate",
            Version = "1.0.0"
        };

        return View(viewModel);
    }

    /// <summary>
    /// GET: Home/Error
    /// Returns error page
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Helper method to track page visits
    /// Demonstrates separation of concerns
    /// </summary>
    private int GetOrIncrementVisitCount()
    {
        const string sessionKey = "VisitCount";

        if (HttpContext.Session.TryGetValue(sessionKey, out byte[]? value))
        {
            var count = int.Parse(System.Text.Encoding.UTF8.GetString(value ?? new byte[0]));
            HttpContext.Session.SetString(sessionKey, (count + 1).ToString());
            return count + 1;
        }

        HttpContext.Session.SetString(sessionKey, "1");
        return 1;
    }
}
