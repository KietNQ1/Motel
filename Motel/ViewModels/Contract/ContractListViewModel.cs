namespace Motel.ViewModels.Contract;

/// <summary>
/// Dòng trong danh sách hợp đồng (trang Index)
/// </summary>
public class ContractListViewModel
{
    public int ContractId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? DaysRemaining { get; set; }

    public string StartDateFormatted => StartDate.ToString("dd/MM/yyyy");
    public string EndDateFormatted => EndDate.ToString("dd/MM/yyyy");
    public string DaysRemainingText => DaysRemaining.HasValue ? $"{DaysRemaining} ngày" : "—";

    public string StatusBadgeClass => Status switch
    {
        "active" => "bg-green-50 text-green-800 dark:bg-green-500/15 dark:text-green-500",
        "ended" => "bg-gray-50 text-gray-800 dark:bg-gray-500/15 dark:text-gray-500",
        _ => "bg-blue-50 text-blue-800 dark:bg-blue-500/15 dark:text-blue-500"
    };

    public string StatusText => Status switch
    {
        "active" => "Đang hiệu lực",
        "ended" => "Đã kết thúc",
        _ => Status
    };

    public string UrgencyBadgeClass => (DaysRemaining ?? 999) switch
    {
        <= 7 => "bg-red-50 text-red-800 dark:bg-red-500/15 dark:text-red-500",
        <= 30 => "bg-amber-50 text-amber-800 dark:bg-amber-500/15 dark:text-amber-500",
        _ => "bg-gray-50 text-gray-600 dark:bg-gray-500/15 dark:text-gray-400"
    };
}
