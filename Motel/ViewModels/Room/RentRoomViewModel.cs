using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Room
{
    public class RentRoomViewModel
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public decimal RentPrice { get; set; }
        public string PropertyName { get; set; } = string.Empty;

        public int MaxOccupants { get; set; } // để validate số người ở ghép

        // Danh sách người ở (nhập tay)
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 người ở")]
        public List<TenantInputViewModel> Occupants { get; set; } = new();

        // Chọn tenant chính bằng index trong Occupants
        [Range(0, int.MaxValue, ErrorMessage = "Chưa chọn người thuê chính")]
        public int PrimaryIndex { get; set; } = 0;

        // Contract
        [Required(ErrorMessage = "Tiền cọc là bắt buộc")]
        [Range(0, 999999999)]
        [Display(Name = "Tiền cọc (VNĐ)")]
        public decimal DepositAmount { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu hợp đồng")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [Display(Name = "Ngày kết thúc hợp đồng")]
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddMonths(6));

        // Initial meter (optional)
        [Display(Name = "Chỉ số điện ban đầu")]
        [Range(0, 999999)]
        public int? InitialElectricReading { get; set; }

        [Display(Name = "Chỉ số nước ban đầu")]
        [Range(0, 999999)]
        public int? InitialWaterReading { get; set; }
    }
}