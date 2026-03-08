using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Room
{
    public class TenantInputViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Display(Name = "CMND/CCCD")]
        public string? IdentityNo { get; set; }
    }
}