using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Room
{
    public class RoomEditViewModel
    {
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Tên phòng là bắt buộc")]
        [Display(Name = "Tên phòng")]
        public string RoomName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá thuê là bắt buộc")]
        [Range(100000, 999999999, ErrorMessage = "Giá thuê phải từ 100,000 đến 999,999,999")]
        [Display(Name = "Giá thuê (VNĐ/tháng)")]
        public decimal RentPrice { get; set; }

        [Range(1, 20, ErrorMessage = "Số người tối đa phải từ 1 đến 20")]
        [Display(Name = "Số người tối đa")]
        public int MaxOccupants { get; set; }
    }
}
