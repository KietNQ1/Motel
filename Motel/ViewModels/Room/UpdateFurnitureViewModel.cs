namespace Motel.ViewModels.Room;

public class UpdateFurnitureViewModel
{
    public int FurnitureId { get; set; }

    public string Name { get; set; } = "";

    public int Quantity { get; set; }

    public string? Description { get; set; }
}