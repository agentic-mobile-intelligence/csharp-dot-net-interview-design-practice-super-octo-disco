namespace HelloWorldMvcApp.Models;

/// <summary>
/// ErrorViewModel - Represents error page data
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
