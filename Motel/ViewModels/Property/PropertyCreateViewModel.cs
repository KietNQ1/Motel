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

        public List<PropertyFeeSettingViewModel> FeeSettings { get; set; } = new List<PropertyFeeSettingViewModel>();
    }

    public class PropertyFeeSettingViewModel
    {
        public int FeeTypeId { get; set; }
        
        [Display(Name = "Tên phí")]
        [Required(ErrorMessage = "Tên phí là bắt buộc")]
        public string FeeTypeName { get; set; } = string.Empty;
        
        [Display(Name = "Cách tính")]
        public string CalculationMethod { get; set; } = "fixed";
        
        [Display(Name = "Đơn giá")]
        public decimal UnitPrice { get; set; }
        
        [Display(Name = "Khối lượng/Mức phí cơ bản")]
        public decimal BaseAmount { get; set; } = 1;
        
        public bool IsSelected { get; set; }

        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; } = "tháng";
    }
}
