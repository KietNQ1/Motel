using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Motel.ViewModels.Payment;

public class PaymentSettingsViewModel
{
    [Display(Name = "Tên ngân hàng")]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Mã ngân hàng (VietQR)")]
    public string BankCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Số tài khoản")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tên chủ tài khoản")]
    public string BankAccountName { get; set; } = string.Empty;

    public List<SelectListItem> BankOptions { get; set; } = new();

    public List<VietQrRequestItemViewModel> VietQrRequests { get; set; } = new();

    public List<PendingCashConfirmItemViewModel> PendingCashConfirms { get; set; } = new();
}

