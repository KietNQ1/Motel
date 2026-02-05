namespace Motel.ViewModels.Dashboard
{
    /// <summary>
    /// Hợp đồng sắp hết hạn hiển thị trên Dashboard
    /// </summary>
    public class ExpiringContractViewModel
    {
        public int ContractId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        
        // Computed properties
        public string EndDateFormatted => EndDate.ToString("dd/MM/yyyy");
        public string DaysRemainingText => $"{DaysRemaining} ngày";
        
        public string UrgencyBadgeClass => DaysRemaining switch
        {
            <= 7 => "bg-red-50 text-red-800 dark:bg-red-500/15 dark:text-red-500",
            <= 15 => "bg-orange-50 text-orange-800 dark:bg-orange-500/15 dark:text-orange-500",
            _ => "bg-yellow-50 text-yellow-800 dark:bg-yellow-500/15 dark:text-yellow-500"
        };
    }
}
