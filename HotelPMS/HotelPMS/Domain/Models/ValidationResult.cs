namespace HotelPMS.Domain.Models;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();

    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);

    public static ValidationResult Success() => new();

    public static ValidationResult Failure(params string[] errors)
    {
        var result = new ValidationResult();
        foreach (var error in errors)
            result.AddError(error);
        return result;
    }
}
