namespace Motel.ViewModels.Invoice;

public sealed class CreateInvoiceViewModel
{
    public int ContractId { get; set; }
    public int RoomId { get; set; }
    public int PeriodMonth { get; set; } // YYYYMM
    public DateOnly DueDate { get; set; }

    /// <summary> Tên phòng (hiển thị khi tạo hóa đơn từ room detail). </summary>
    public string? RoomName { get; set; }
    
    public decimal RentPrice { get; set; }

    public List<InvoiceFeeItemVm> FeeItems { get; set; } = new();

    public List<ExtraChargeVm> ExtraCharges { get; set; } = new();
}

public sealed class InvoiceFeeItemVm
{
    public int FeeTypeId { get; set; }
    public string FeeTypeName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = "fixed";
    public decimal UnitPrice { get; set; }
    public decimal BaseAmount { get; set; }

    // For meter method
    public int? PreviousReading { get; set; }
    public int? CurrentReading { get; set; }

    // For per_person/per_room method
    public int? Quantity { get; set; }
}

public sealed class ExtraChargeVm
{
    public string ItemType { get; set; } = "other"; // other/late_fee/repair...
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}
