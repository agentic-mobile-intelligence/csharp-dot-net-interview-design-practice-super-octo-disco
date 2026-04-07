namespace HotelPMS.Domain.Models;

public class NOIResult
{
    public string PropertyId { get; set; } = string.Empty;
    public DateOnly ReportDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal NOI => TotalRevenue - OperatingExpenses;
    public decimal NOIMargin => TotalRevenue == 0 ? 0 : NOI / TotalRevenue;

    // Sector-specific breakdowns
    public Dictionary<string, decimal> RevenueBreakdown { get; set; } = new();
    public Dictionary<string, decimal> ExpenseBreakdown { get; set; } = new();
}
