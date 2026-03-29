using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Tenant
{
    public class TenantEditViewModel
    {
        public int TenantId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(150)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(50)]
        [Display(Name = "CCCD/CMND")]
        public string? IdentityNo { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateOnly? DateOfBirth { get; set; }

        [StringLength(500)]
        [Display(Name = "Nơi ĐKTT")]
        public string? PermanentAddress { get; set; }

        public int? CccdFrontImageId { get; set; }
        public int? CccdBackImageId { get; set; }

        public int? ResidenceProofImageId { get; set; }

        [Display(Name = "Đã đăng ký tạm trú")]
        public bool IsTemporaryResidenceRegistered { get; set; }

        // redirect back after save
        public int? ReturnPropertyId { get; set; }
    }
}
