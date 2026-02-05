namespace Motel.ViewModels.Dashboard
{
    /// <summary>
    /// Dữ liệu cho biểu đồ doanh thu 12 tháng
    /// </summary>
    public class MonthlyRevenueChartViewModel
    {
        public List<string> Months { get; set; } = new();
        public List<decimal> Revenues { get; set; } = new();
        
        // JSON serialized cho JavaScript
        public string MonthsJson => System.Text.Json.JsonSerializer.Serialize(Months);
        public string RevenuesJson => System.Text.Json.JsonSerializer.Serialize(Revenues);
    }
}
