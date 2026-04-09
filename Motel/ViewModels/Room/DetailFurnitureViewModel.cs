using Motel.ViewModels.Room;

namespace Motel.ViewModels.Room;

public class DetailFurnitureViewModel
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public List<RoomFurnitureViewModel> Furnitures { get; set; } = new();
}