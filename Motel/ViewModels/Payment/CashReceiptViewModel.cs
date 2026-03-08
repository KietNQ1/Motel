

namespace Motel.ViewModels.Payment;

public class CashReceiptViewModel
{
        public int PaymentId { get; set; }
        public string ProviderTxnId { get; set; } = "";
        public DateTime PaidAt { get; set; }

        public string TenantName { get; set; } = "";
        public string TenantPhone { get; set; } = "";

        public string RoomName { get; set; } = "";
        public string PropertyName { get; set; } = "";

        public int PeriodMonth { get; set; }
        public DateOnly DueDate { get; set; }

        public decimal TotalAmount { get; set; }

        public List<ReceiptLineVm> Lines { get; set; } = new();
    

    public class ReceiptLineVm
    {
        public string ItemType { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
