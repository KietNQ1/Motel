using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Property
{
    public class PropertyCreateViewModel
    {
        [Required(ErrorMessage = "Tên nhà trọ là bắt buộc")]
        [StringLength(150, ErrorMessage = "Tên nhà trọ không được vượt quá 150 ký tự")]
        [Display(Name = "Tên nhà trọ")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // Utility Settings - Default values for all rooms
        [Required(ErrorMessage = "Giá điện là bắt buộc")]
        [Range(0, 999999, ErrorMessage = "Giá điện phải từ 0 đến 999,999")]
        [Display(Name = "Giá điện (VNĐ/số)")]
        public decimal ElectricUnitPrice { get; set; } = 3500;

        [Required(ErrorMessage = "Giá nước là bắt buộc")]
        [Range(0, 999999, ErrorMessage = "Giá nước phải từ 0 đến 999,999")]
        [Display(Name = "Giá nước (VNĐ/số hoặc VNĐ/người)")]
        public decimal WaterUnitPrice { get; set; } = 15000;

        [Display(Name = "Cách tính tiền nước")]
        public WaterChargeType WaterChargeType { get; set; } = WaterChargeType.PerUnit;

        [Range(0, 999999, ErrorMessage = "Phí Internet phải từ 0 đến 999,999")]
        [Display(Name = "Phí Internet (VNĐ/tháng)")]
        public decimal InternetFee { get; set; } = 100000;

        [Range(0, 999999, ErrorMessage = "Phí rác phải từ 0 đến 999,999")]
        [Display(Name = "Phí rác (VNĐ/tháng)")]
        public decimal TrashFee { get; set; } = 30000;
    }

    public enum WaterChargeType
    {
        [Display(Name = "Theo số nước (đồng hồ)")]
        PerUnit = 0,
        
        [Display(Name = "Theo đầu người")]
        PerPerson = 1
    }
}
