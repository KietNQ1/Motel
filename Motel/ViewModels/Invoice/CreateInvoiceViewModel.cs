namespace Motel.ViewModels.Invoice;

public sealed class CreateInvoiceViewModel
{
    public int ContractId { get; set; }
    public int PeriodMonth { get; set; } // YYYYMM
    public DateOnly DueDate { get; set; }

    /// <summary> Tên phòng (hiển thị khi tạo hóa đơn từ room detail). </summary>
    public string? RoomName { get; set; }

    // Nếu đã có MeterReadings trong DB thì có thể để null để service tự lấy
    public int? ElectricOld { get; set; }
    public int? ElectricNew { get; set; }
    public int? WaterOld { get; set; }
    public int? WaterNew { get; set; }

    // Nước theo người (tuỳ chọn)
    public int? WaterPeopleCount { get; set; }
    public decimal? WaterPricePerPerson { get; set; }

    public List<ExtraChargeVm> ExtraCharges { get; set; } = new();
}

public sealed class ExtraChargeVm
{
    public string ItemType { get; set; } = "other"; // other/late_fee/repair...
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}
