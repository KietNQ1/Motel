using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Room;

public class AddFurnitureViewModel
{
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên nội thất.")]
    public string Name { get; set; } = "";

    [Range(1, 100, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int Quantity { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public List<IFormFile>? Images { get; set; }
}