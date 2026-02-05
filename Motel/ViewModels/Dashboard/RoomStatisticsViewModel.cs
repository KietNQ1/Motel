namespace Motel.ViewModels.Dashboard
{
    /// <summary>
    /// Thống kê về phòng trọ cho Dashboard
    /// </summary>
    public class RoomStatisticsViewModel
    {
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public decimal OccupancyRate { get; set; }
        
        // Formatted properties for view
        public string OccupancyRateFormatted => $"{OccupancyRate:F1}%";
        public string AvailableRoomsText => $"{AvailableRooms} trống";
    }
}
