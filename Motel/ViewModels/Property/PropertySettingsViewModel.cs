using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Property
{
    public class PropertySettingsViewModel
    {
        public int PropertyId { get; set; }

        [Required(ErrorMessage = "Tên nhà trọ không được để trống")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<PropertyFeeSettingItemViewModel> FeeSettings { get; set; } = new();
    }

    public class PropertyFeeSettingItemViewModel
    {
        public int FeeSettingId { get; set; }
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        public string CalculationMethod { get; set; } = "fixed";
        public decimal UnitPrice { get; set; }
        public decimal BaseAmount { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }
}
