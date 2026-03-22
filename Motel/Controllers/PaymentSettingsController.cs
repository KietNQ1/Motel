using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Helpers;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.ViewModels.Payment;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Motel.Controllers;

[Authorize]
public class PaymentSettingsController : Controller
{
    private readonly MotelDbContext _db;
    private readonly LandlordHelper _landlordHelper;
    private readonly IPaymentRepository _paymentRepo;
    private static readonly (string Code, string Name)[] VietQrBanks =
    {
        ("970436", "Vietcombank"),
        ("970418", "BIDV"),
        ("970422", "MB Bank"),
        ("970407", "Techcombank"),
        ("970416", "ACB"),
        ("970432", "VPBank"),
        ("970441", "VIB"),
        ("970423", "TPBank"),
    };

    public PaymentSettingsController(MotelDbContext db, LandlordHelper landlordHelper, IPaymentRepository paymentRepo)
    {
        _db = db;
        _landlordHelper = landlordHelper;
        _paymentRepo = paymentRepo;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        if (landlordId == 0)
        {
            return RedirectToAction("Index", "Home");
        }

        var vm = await BuildViewModelAsync(landlordId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PaymentSettingsViewModel vm)
    {
        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine("CurrentUserId: " + userId);
        Console.WriteLine("LandlordId: " + landlordId);

        if (landlordId == 0)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            await FillBankAndPaymentQueuesAsync(vm, landlordId);
            return View(vm);
        }

        // ✅ ĐOẠN BẠN HỎI – ĐẶT Ở ĐÂY
        var bankMeta = VietQrBanks.FirstOrDefault(b =>
            string.Equals(b.Code, vm.BankCode?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(bankMeta.Code))
        {
            ModelState.AddModelError(nameof(vm.BankCode), "Mã ngân hàng không hợp lệ. Vui lòng chọn trong danh sách.");

            await FillBankAndPaymentQueuesAsync(vm, landlordId);
            return View(vm);
        }

        // Chuẩn hóa lại code & name theo danh sách
        vm.BankCode = bankMeta.Code;
        vm.BankName = bankMeta.Name;
        // ✅ HẾT ĐOẠN BANKMETA

        var existing = await _db.LandlordBankAccounts
            .Where(x => x.LandlordId == landlordId && !x.IsDeleted)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.LandlordBankAccountId)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            var bank = new LandlordBankAccount
            {
                LandlordId = landlordId,
                BankName = vm.BankName.Trim(),
                BankCode = vm.BankCode.Trim(),
                BankAccountNumber = vm.BankAccountNumber.Trim(),
                BankAccountName = vm.BankAccountName.Trim(),
                IsPrimary = true,
                IsDeleted = false
            };
            _db.LandlordBankAccounts.Add(bank);
        }
        else
        {
            existing.BankName = vm.BankName.Trim();
            existing.BankCode = vm.BankCode.Trim();
            existing.BankAccountNumber = vm.BankAccountNumber.Trim();
            existing.BankAccountName = vm.BankAccountName.Trim();
            existing.IsPrimary = true;
            existing.IsDeleted = false;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu cấu hình tài khoản ngân hàng cho VietQR.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PaymentSettingsViewModel> BuildViewModelAsync(int landlordId)
    {
        var bank = await _db.LandlordBankAccounts
            .AsNoTracking()
            .Where(x => x.LandlordId == landlordId && !x.IsDeleted)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.LandlordBankAccountId)
            .FirstOrDefaultAsync();

        var vm = new PaymentSettingsViewModel();
        if (bank != null)
        {
            vm.BankName = bank.BankName;
            vm.BankCode = bank.BankCode;
            vm.BankAccountNumber = bank.BankAccountNumber;
            vm.BankAccountName = bank.BankAccountName;
        }

        await FillBankAndPaymentQueuesAsync(vm, landlordId);
        return vm;
    }

    private async Task FillBankAndPaymentQueuesAsync(PaymentSettingsViewModel vm, int landlordId)
    {
        vm.BankOptions = VietQrBanks
            .Select(b => new SelectListItem
            {
                Value = b.Code,
                Text = $"{b.Name} ({b.Code})",
                Selected = string.Equals(b.Code, vm.BankCode, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        var vietQr = await _paymentRepo.GetVietQrRequestsForLandlordAsync(landlordId);
        vm.VietQrRequests = vietQr.Select(p => new VietQrRequestItemViewModel
        {
            PaymentIntentId = p.PaymentIntentId,
            InvoiceId = p.InvoiceId,
            RoomName = p.Invoice.Room.RoomName,
            PropertyName = p.Invoice.Room.Property.Name,
            TenantName = p.Invoice.Contract.Tenant.FullName,
            Amount = p.Amount,
            CreatedAt = p.CreatedAt
        }).ToList();

        var cash = await _paymentRepo.GetPendingCashIntentsForLandlordAsync(landlordId);
        vm.PendingCashConfirms = cash.Select(p => new PendingCashConfirmItemViewModel
        {
            PaymentIntentId = p.PaymentIntentId,
            InvoiceId = p.InvoiceId,
            RoomName = p.Invoice.Room.RoomName,
            PropertyName = p.Invoice.Room.Property.Name,
            TenantName = p.Invoice.Contract.Tenant.FullName,
            Amount = p.Amount,
            CreatedAt = p.CreatedAt
        }).ToList();
    }
}

