using HotelPMS.Domain.Models;

namespace HotelPMS.Calculators;

/// <summary>
/// Hotel NOI = Room Revenue + Other Revenue - Operating Expenses.
/// Breaks revenue down by room type.
/// </summary>
public class HotelNOICalculator : INOICalculator
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
            var roomRevenue = row.RoomRevenue ?? 0m;
            var otherRevenue = row.OtherRevenue ?? 0m;
            var expenses = row.OperatingExpenses ?? 0m;
            var roomType = row.RoomType ?? "Unknown";

            result.TotalRevenue += roomRevenue + otherRevenue;
            result.OperatingExpenses += expenses;

            result.RevenueBreakdown.TryGetValue(roomType, out var existing);
            result.RevenueBreakdown[roomType] = existing + roomRevenue;
        }

        return result;
    }
}
