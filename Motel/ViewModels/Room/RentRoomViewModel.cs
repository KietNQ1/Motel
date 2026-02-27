using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Room
{
    public class RentRoomViewModel
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public decimal RentPrice { get; set; }
        public string PropertyName { get; set; } = string.Empty;

        // Tenant Information
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        [Display(Name = "Họ tên người thuê")]
        public string TenantFullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? TenantPhone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string? TenantEmail { get; set; }

        [StringLength(20, ErrorMessage = "Số CMND/CCCD không được quá 20 ký tự")]
        [Display(Name = "Số CMND/CCCD")]
        public string? IdentityNo { get; set; }

        // Contract Information
        [Required(ErrorMessage = "Tiền cọc là bắt buộc")]
        [Range(0, 999999999, ErrorMessage = "Tiền cọc phải từ 0 đến 999,999,999")]
        [Display(Name = "Tiền cọc (VNĐ)")]
        public decimal DepositAmount { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu hợp đồng")]
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [Display(Name = "Ngày kết thúc hợp đồng")]
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddMonths(6));

        // Initial Meter Readings (optional)
        [Display(Name = "Chỉ số điện ban đầu")]
        [Range(0, 999999, ErrorMessage = "Chỉ số điện phải từ 0 đến 999,999")]
        public int? InitialElectricReading { get; set; } = 0;

        [Display(Name = "Chỉ số nước ban đầu")]
        [Range(0, 999999, ErrorMessage = "Chỉ số nước phải từ 0 đến 999,999")]
        public int? InitialWaterReading { get; set; } = 0;
    }
}
