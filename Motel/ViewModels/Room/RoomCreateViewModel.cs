using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Room
{
    public class RoomCreateViewModel
    {
        [Required]
        public int PropertyId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phòng")]
        [Display(Name = "Tên phòng")]
        public string RoomName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá phòng")]
        [Display(Name = "Giá phòng (VNĐ/tháng)")]
        public decimal RentPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số người tối đa")]
        [Display(Name = "Số người tối đa")]
        public int MaxOccupants { get; set; } = 2; // default
        
        // This is only used for guiding the validation back to the right context if it fails
        public int Floor { get; set; }

        [Display(Name = "Hình ảnh phòng (Bao gồm: phòng ngủ, nhà vệ sinh, nhà bếp, ban công...)")]
        public List<IFormFile>? Images { get; set; }
    }
}
