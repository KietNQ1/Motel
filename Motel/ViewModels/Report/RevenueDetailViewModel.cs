namespace Motel.ViewModels.Report;

/// <summary>
/// Trang doanh thu chi tiết 12 tháng
/// </summary>
public class RevenueDetailViewModel
{
    public int Year { get; set; }
    public int? PropertyId { get; set; }
    public List<PropertyItemViewModel> Properties { get; set; } = new();
    /// <summary> Tháng (1-12) -> Doanh thu đã thu (paid)
    /// </summary>
    public List<MonthRevenueRow> Months { get; set; } = new();
    public decimal TotalRevenue => Months.Sum(m => m.Revenue);
    public string TotalRevenueFormatted => $"{TotalRevenue:N0}đ";
}

public class MonthRevenueRow
{
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty; // "Tháng 1/2025"
    public decimal Revenue { get; set; }
    public string RevenueFormatted => $"{Revenue:N0}đ";
}

public class PropertyItemViewModel
{
    public int PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
}
