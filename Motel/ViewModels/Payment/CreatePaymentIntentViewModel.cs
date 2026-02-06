using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Motel.ViewModels.Payments;

public class CreatePaymentIntentViewModel
{
    [Required]
    public int InvoiceId { get; set; }

    [Required]
    public string Provider { get; set; } = "CASH"; // CASH/VNPAY/PAYOS/MOMO

    // hiển thị thôi (lấy từ invoice)
    public decimal Amount { get; set; }

    public DateTime? DueDate { get; set; }
    public string? Note { get; set; }

    // dropdown data
    public List<SelectListItem> InvoiceOptions { get; set; } = new();
}
