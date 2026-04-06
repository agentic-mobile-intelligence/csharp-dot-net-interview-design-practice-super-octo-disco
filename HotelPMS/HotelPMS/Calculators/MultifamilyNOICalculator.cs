using HotelPMS.Domain.Models;

namespace HotelPMS.Calculators;

/// <summary>
/// Multifamily NOI = Annual Rent + Other Revenue - Operating Expenses.
/// Annualizes monthly rent for a full-year view.
/// </summary>
public class MultifamilyNOICalculator : INOICalculator
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
            var annualRent = (row.MonthlyRent ?? 0m) * 12m;
            var otherRevenue = row.OtherRevenue ?? 0m;
            var expenses = row.OperatingExpenses ?? 0m;

            result.TotalRevenue += annualRent + otherRevenue;
            result.OperatingExpenses += expenses;

            result.RevenueBreakdown["RentalIncome"] =
                result.RevenueBreakdown.GetValueOrDefault("RentalIncome") + annualRent;
        }

        return result;
    }
}
