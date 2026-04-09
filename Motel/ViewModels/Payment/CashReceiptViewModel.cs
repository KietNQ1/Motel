using Motel.Services;

namespace Motel.ViewModels.Payment;

public class CashReceiptViewModel
{
        public int PaymentId { get; set; }
        public string ProviderTxnId { get; set; } = "";
        public DateTime PaidAt { get; set; }

        /// <summary>Giá trị lưu DB: cash, vietqr, …</summary>
        public string Provider { get; set; } = "";

        public string TenantName { get; set; } = "";
        public string TenantPhone { get; set; } = "";

        public string RoomName { get; set; } = "";
        public string PropertyName { get; set; } = "";

        public int PeriodMonth { get; set; }
        public DateOnly DueDate { get; set; }

        public decimal TotalAmount { get; set; }

        public List<ReceiptLineVm> Lines { get; set; } = new();

        public string PaymentMethodLabel
        {
            get
            {
                var p = (Provider ?? "").Trim().ToLowerInvariant();
                return p switch
                {
                    PaymentProviders.VIETQR => "VietQR (chuyển khoản)",
                    PaymentProviders.CASH => "Tiền mặt",
                    PaymentProviders.PAYOS => "PayOS",
                    _ => string.IsNullOrWhiteSpace(Provider) ? "—" : Provider
                };
            }
        }

        public string PaymentMethodIcon
        {
            get
            {
                var p = (Provider ?? "").Trim().ToLowerInvariant();
                return p == PaymentProviders.VIETQR ? "qr_code_2" : "payments";
            }
        }
    

    public class ReceiptLineVm
    {
        public string ItemType { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
