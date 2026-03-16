using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Tenant
{
    public class TenantCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? IdentityNo { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateOnly? DateOfBirth { get; set; }

        [StringLength(500)]
        [Display(Name = "Nơi ĐKTT")]
        public string? PermanentAddress { get; set; }

        // optional: sau khi tạo xong quay về room/contract
        public int? ReturnRoomId { get; set; }
        public int? ReturnPropertyId { get; set; }
    }
}