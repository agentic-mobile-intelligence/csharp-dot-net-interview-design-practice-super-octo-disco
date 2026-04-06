using HotelPMS.Domain.Models;

namespace HotelPMS.Calculators;

/// <summary>
/// Senior living NOI = Care Revenue + Other Revenue - Operating Expenses.
/// Breaks revenue down by care level (Independent, Assisted, Memory, SkilledNursing).
/// </summary>
public class SeniorLivingNOICalculator : INOICalculator
{
    public NOIResult Calculate(IEnumerable<PropertyRow> rows)
    {
        var rowList = rows.ToList();
        var result = new NOIResult
        {
            PropertyId = rowList.FirstOrDefault()?.PropertyId ?? string.Empty,
            ReportDate = rowList.FirstOrDefault()?.ReportDate ?? default
        };

        foreach (var row in rowList)
        {
            var careRevenue = row.CareRevenue ?? 0m;
            var otherRevenue = row.OtherRevenue ?? 0m;
            var expenses = row.OperatingExpenses ?? 0m;
            var careLevel = row.CareLevel ?? "Unknown";

            result.TotalRevenue += careRevenue + otherRevenue;
            result.OperatingExpenses += expenses;

            result.RevenueBreakdown[careLevel] =
                result.RevenueBreakdown.GetValueOrDefault(careLevel) + careRevenue;
        }

        return result;
    }
}
