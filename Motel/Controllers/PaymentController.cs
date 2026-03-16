using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Motel.Repositories.Interface;
using Motel.Services;
using Motel.Services.Interface;
using Motel.ViewModels.Payment;
using static Motel.ViewModels.Payment.CashReceiptViewModel;
using Motel.Helpers;
using Motel.Models;

namespace Motel.Controllers;

public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IInvoiceRepository _invoiceRepo; // ✅ thêm
    private readonly LandlordHelper _landlordHelper;

    public PaymentController(
        IPaymentService paymentService,
        IPaymentRepository paymentRepo,
        IInvoiceRepository invoiceRepo,
        LandlordHelper landlordHelper)
    {
        _paymentService = paymentService;
        _paymentRepo = paymentRepo;
        _invoiceRepo = invoiceRepo;
        _landlordHelper = landlordHelper;
    }

   


    // GET: /Payment/CreateIntent?invoiceId=1
    [HttpGet]
    public async Task<IActionResult> CreateIntent(int invoiceId)
    {
        var inv = await _invoiceRepo.GetByIdWithLinesAsync(invoiceId);
        if (inv == null) return NotFound();

        // (optional) chặn invoice đã paid
        // if (inv.Status == InvoiceStatus.Paid) return BadRequest("Invoice already paid");

        var vm = new CreatePaymentIntentViewModel
        {
            InvoiceId = inv.InvoiceId,
            Amount = inv.TotalAmount, // nhớ đúng field
            Provider = PaymentProviders.CASH // default tuỳ bạn
        };

        // Nếu UI bạn cần dropdown thì build 1 option cũng được
        vm.InvoiceOptions = new List<SelectListItem>
        {
            new SelectListItem
            {
                Value = inv.InvoiceId.ToString(),
                Text = $"Invoice #{inv.InvoiceId} - {inv.TotalAmount:n0} VND",
                Selected = true
            }
        };

        return View(vm);
    }

    // POST: /Payment/CreateIntent
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIntent(CreatePaymentIntentViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            vm.Provider = vm.Provider?.Trim().ToLower();

            var intent = await _paymentService.CreateIntentAsync(vm.InvoiceId, vm.Provider);

            if (vm.Provider == PaymentProviders.CASH)
            {
                return RedirectToAction(nameof(CashConfirm), new { paymentIntentId = intent.PaymentIntentId });
            }

            if (vm.Provider == PaymentProviders.VIETQR)
            {
                return RedirectToAction(nameof(VietQr), new { paymentIntentId = intent.PaymentIntentId });
            }

            // Online khác (PayOS, v.v...): tạm thời chưa implement gateway => trả về trang "chưa hỗ trợ"
            return RedirectToAction(nameof(IntentCreated), new { id = intent.PaymentIntentId });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = ex.Message,
                invoiceId = vm.InvoiceId
            });
        }
    }

    // GET: /Payment/IntentCreated/5
    [HttpGet]
    public IActionResult IntentCreated(int id)
    {
        ViewBag.IntentId = id;
        return View();
    }

    // GET: /Payment/CashConfirm?paymentIntentId=5
    [HttpGet]
    public async Task<IActionResult> CashConfirm(int paymentIntentId)
    {
        var intent = await _paymentRepo.GetIntentAsync(paymentIntentId);
        if (intent == null)
            return NotFound();

        ViewBag.PaymentIntentId = intent.PaymentIntentId;
        ViewBag.InvoiceId = intent.InvoiceId;
        ViewBag.Amount = intent.Amount.ToString("N0") + " đ";

        ViewBag.RoomName = intent.Invoice?.Room?.RoomName ?? "Chưa có phòng";
        ViewBag.TenantName = intent.Invoice?.Contract?.Tenant?.FullName ?? "Chưa có người thuê";
        return View();
    }

    // GET: /Payment/VietQr?paymentIntentId=5
    [HttpGet]
    public async Task<IActionResult> VietQr(int paymentIntentId)
    {
        var intent = await _paymentRepo.GetIntentAsync(paymentIntentId);
        if (intent == null) return NotFound();

        var invoice = intent.Invoice;
        if (invoice == null) return NotFound();

        var room = invoice.Room;
        var property = room?.Property;
        var landlord = property?.Landlord;
        var bank = landlord?.LandlordBankAccounts
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.LandlordBankAccountId)
            .FirstOrDefault();

        if (landlord == null || bank == null)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Chủ trọ chưa cấu hình thông tin tài khoản ngân hàng cho VietQR.",
                invoiceId = invoice.InvoiceId
            });
        }

        // var amountVnd = (long)decimal.Round(intent.Amount, 0, MidpointRounding.AwayFromZero);
        // // Nội dung rõ ràng cho người thuê & chủ trọ
        // var transferContent = $"Thanh toan hoa don {invoice.InvoiceId}";
        var amountVnd = (long)decimal.Round(intent.Amount, 0, MidpointRounding.AwayFromZero);

        // Ví dụ nội dung chi tiết: HĐ 5 - P101 - 3/2024
        var roomName = room?.RoomName ?? "";
        var period = invoice.PeriodMonth; // dạng yyyymm
        var transferContent = $"HD {invoice.InvoiceId} - {roomName} - {period}";

        var encodedContent = Uri.EscapeDataString(transferContent);
        var qrImageUrl =
            $"https://img.vietqr.io/image/{bank.BankCode}-{bank.BankAccountNumber}-qr_only.png?amount={amountVnd}&addInfo={encodedContent}";

        var vm = new VietQrPaymentViewModel
        {
            InvoiceId = invoice.InvoiceId,
            PaymentIntentId = intent.PaymentIntentId,
            Amount = intent.Amount,
            TransferContent = transferContent,
            LandlordName = landlord.DisplayName,
            BankName = bank.BankName,
            BankAccountNumber = bank.BankAccountNumber,
            BankAccountName = bank.BankAccountName,
            BankCode = bank.BankCode,
            QrImageUrl = qrImageUrl,
            RoomName = room?.RoomName,
            PropertyName = property?.Name,
            TenantName = invoice.Contract?.Tenant?.FullName
        };

        return View(vm);
    }


    // POST: /Payment/CashConfirm
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CashConfirmPost(int paymentIntentId)
    {
        // TODO: lấy confirmedByUserId từ login/claims, tạm để 1
        var payment = await _paymentService.ConfirmCashAsync(
            paymentIntentId,
            confirmedByUserId: 1
        );
        // Về trang biên nhận thanh toán
        return RedirectToAction(
            nameof(CashReceipt),
            new { paymentId = payment.PaymentId }
        );
    }


    // GET: /Payment/CashReceipt?paymentId=5
    [HttpGet]
    public async Task<IActionResult> CashReceipt(int paymentId)
    {
        var payment = await _paymentRepo.GetPaymentForReceiptAsync(paymentId);
        if (payment == null) return NotFound();

        var invoice = payment.Invoice;

        var vm = new CashReceiptViewModel
        {
            PaymentId = payment.PaymentId,
            ProviderTxnId = payment.ProviderTxnId,
            PaidAt = payment.PaidAt,

            TenantName = invoice.Contract.Tenant.FullName,
            TenantPhone = invoice.Contract.Tenant.Phone,

            // ✅ sửa đúng navigation theo model Invoice của bạn
            RoomName = invoice.Room.RoomName,
            PropertyName = invoice.Room.Property.Name,

            PeriodMonth = invoice.PeriodMonth,
            DueDate = invoice.DueDate,

            TotalAmount = payment.Amount,

            // ✅ thêm lines (điện/nước/internet/rác/phát sinh…)
            Lines = invoice.InvoiceLines.Select(l => new ReceiptLineVm
            {
                ItemType = l.FeeType?.Name ?? "",
                Description = string.IsNullOrEmpty(l.Description) ? (l.FeeType?.Name ?? "") : l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal ?? (l.Quantity * l.UnitPrice)
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult PaymentNotice(string message, int? invoiceId)
    {
        ViewBag.Message = message;
        ViewBag.InvoiceId = invoiceId;
        return View();
    }

    // POST: /Payment/VietQrTenantConfirmed
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VietQrTenantConfirmed(int paymentIntentId)
    {
        var intent = await _paymentRepo.GetIntentAsync(paymentIntentId);
        if (intent == null)
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Payment intent không tồn tại.",
                invoiceId = (int?)null
            });

        if (intent.Provider != PaymentProviders.VIETQR)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Phương thức thanh toán không hợp lệ.",
                invoiceId = intent.InvoiceId
            });
        }

        if (intent.ExpiredAt.HasValue && intent.ExpiredAt.Value <= DateTime.UtcNow)
        {
            intent.Status = PaymentIntentStatus.Expired;
            await _paymentRepo.SaveChangesAsync();

            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Yêu cầu thanh toán đã hết hạn, vui lòng tạo lại.",
                invoiceId = intent.InvoiceId
            });
        }

        // Đánh dấu intent đã được người thuê báo là đã chuyển khoản.
        // Vẫn chưa tạo Payment, chờ chủ trọ xác nhận thủ công.
        intent.Status = PaymentIntentStatus.Succeeded;
        await _paymentRepo.SaveChangesAsync();

        return RedirectToAction(nameof(PaymentNotice), new
        {
            message = "Cảm ơn bạn. Thanh toán của bạn đang chờ chủ trọ xác nhận.",
            invoiceId = intent.InvoiceId
        });
    }

    // POST: /Payment/VietQrLandlordConfirm
    // Action này dành cho chủ trọ, sau khi kiểm tra trong app ngân hàng.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VietQrLandlordConfirm(int paymentIntentId)
    {
        try
        {
            // TODO: lấy userId thực từ Identity
            var payment = await _paymentService.ConfirmVietQrAsync(paymentIntentId, confirmedByUserId: 1);

            return RedirectToAction(
                nameof(CashReceipt),
                new { paymentId = payment.PaymentId }
            );
        }
        catch (Exception ex)
        {
            var intent = await _paymentRepo.GetIntentAsync(paymentIntentId);
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = ex.Message,
                invoiceId = intent?.InvoiceId
            });
        }
    }

    // POST: /Payment/VietQrLandlordReject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VietQrLandlordReject(int paymentIntentId)
    {
        var intent = await _paymentRepo.GetIntentAsync(paymentIntentId);
        if (intent == null)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Payment intent không tồn tại.",
                invoiceId = (int?)null
            });
        }

        if (intent.Provider != PaymentProviders.VIETQR)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Phương thức thanh toán không hợp lệ.",
                invoiceId = intent.InvoiceId
            });
        }

        // Tạo bản ghi Payment với trạng thái Rejected để lưu lịch sử
        var payment = new Payment
        {
            InvoiceId = intent.InvoiceId,
            PaymentIntentId = intent.PaymentIntentId,
            Provider = PaymentProviders.VIETQR,
            ProviderTxnId = $"VIETQR-REJECT-{DateTime.UtcNow:yyyyMMddHHmmss}-INTENT{intent.PaymentIntentId}",
            Amount = intent.Amount,
            PaidAt = DateTime.UtcNow,
            Status = PaymentStatus.Rejected,
            RawCallbackJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "vietqr_rejected",
                rejectedAt = DateTime.UtcNow
            })
        };

        await _paymentRepo.AddPaymentAsync(payment);
        await _paymentRepo.SaveChangesAsync();

        return RedirectToAction(nameof(PaymentNotice), new
        {
            message = "Yêu cầu thanh toán đã bị từ chối.",
            invoiceId = intent.InvoiceId
        });
    }

    // GET: /Payment/VietQrRequests
    [HttpGet]
    public async Task<IActionResult> VietQrRequests()
    {
        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        if (landlordId == 0)
        {
            return RedirectToAction("Index", "Home");
        }

        var intents = await _paymentRepo.GetVietQrRequestsForLandlordAsync(landlordId);

        var list = intents.Select(p => new VietQrRequestItemViewModel
        {
            PaymentIntentId = p.PaymentIntentId,
            InvoiceId = p.InvoiceId,
            RoomName = p.Invoice.Room.RoomName,
            PropertyName = p.Invoice.Room.Property.Name,
            TenantName = p.Invoice.Contract.Tenant.FullName,
            Amount = p.Amount,
            CreatedAt = p.CreatedAt
        }).ToList();

        return View(list);
    }
}
