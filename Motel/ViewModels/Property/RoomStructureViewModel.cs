using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Property
{
    public class RoomStructureViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số tầng là bắt buộc")]
        [Range(1, 50, ErrorMessage = "Số tầng phải từ 1 đến 50")]
        [Display(Name = "Số tầng")]
        public int NumberOfFloors { get; set; } = 1;

        [Required(ErrorMessage = "Số phòng mỗi tầng là bắt buộc")]
        [Display(Name = "Số phòng mỗi tầng")]
        public List<int> RoomsPerFloor { get; set; } = new List<int>();

        [Required(ErrorMessage = "Giá thuê mặc định là bắt buộc")]
        [Range(100000, 999999999, ErrorMessage = "Giá thuê phải từ 100,000 đến 999,999,999")]
        [Display(Name = "Giá thuê mặc định (VNĐ/tháng)")]
        public decimal DefaultRentPrice { get; set; } = 2500000;

        // Utility settings from Property creation (stored in TempData)
        public decimal ElectricUnitPrice { get; set; }
        public decimal WaterUnitPrice { get; set; }
        public decimal InternetFee { get; set; }
        public decimal TrashFee { get; set; }
    }
}
