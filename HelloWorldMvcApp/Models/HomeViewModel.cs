namespace HelloWorldMvcApp.Models;

/// <summary>
/// HomeViewModel - Represents data passed to Home/Index view
/// Demonstrates separation of concerns between Controller and View
/// </summary>
public class HomeViewModel
{
    public string? Title { get; set; }
    public string? Message { get; set; }
    public int VisitCount { get; set; }
}
