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
                return RedirectToAction(nameof(CashPaymentSubmitted), new
                {
                    invoiceId = vm.InvoiceId,
                    paymentIntentId = intent.PaymentIntentId
                });
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

    // GET: /Payment/CashPaymentSubmitted — người thuê sau khi chọn tiền mặt
    [HttpGet]
    public IActionResult CashPaymentSubmitted(int invoiceId, int paymentIntentId)
    {
        ViewBag.InvoiceId = invoiceId;
        ViewBag.PaymentIntentId = paymentIntentId;
        return View();
    }

    // GET: /Payment/CashConfirm?paymentIntentId=5 — chỉ chủ trọ (đúng tài sản) mới xem và xác nhận
    [HttpGet]
    public async Task<IActionResult> CashConfirm(int paymentIntentId)
    {
        var intent = await _paymentRepo.GetIntentAsync(paymentIntentId);
        if (intent == null)
            return NotFound();

        if (intent.Provider != PaymentProviders.CASH)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Yêu cầu không phải thanh toán tiền mặt.",
                invoiceId = intent.InvoiceId
            });
        }

        var succeeded = intent.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Succeeded);
        if (succeeded != null)
            return RedirectToAction(nameof(CashReceipt), new { paymentId = succeeded.PaymentId });

        if (intent.Status != PaymentIntentStatus.Pending)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Yêu cầu tiền mặt này không còn chờ xác nhận.",
                invoiceId = intent.InvoiceId
            });
        }

        if (intent.ExpiredAt.HasValue && intent.ExpiredAt.Value <= DateTime.UtcNow)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Yêu cầu thanh toán đã hết hạn.",
                invoiceId = intent.InvoiceId
            });
        }

        var propLandlordId = intent.Invoice?.Room?.Property?.LandlordId ?? 0;
        if (propLandlordId == 0)
            return NotFound();

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action(nameof(CashConfirm), "Payment", new { paymentIntentId })
            });
        }

        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        if (landlordId == 0 || landlordId != propLandlordId)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Chỉ chủ trọ mới có thể xác nhận đã nhận tiền mặt. Người thuê vui lòng giao tiền trực tiếp và chờ chủ trọ ghi nhận trên hệ thống.",
                invoiceId = intent.InvoiceId
            });
        }

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

        var succeededPayment = intent.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Succeeded);
        if (succeededPayment != null)
            return RedirectToAction(nameof(CashReceipt), new { paymentId = succeededPayment.PaymentId });

        var hasFinalPaymentRecord = intent.Payments.Any(p =>
            p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Rejected);
        if (intent.Provider == PaymentProviders.VIETQR && !hasFinalPaymentRecord &&
            (intent.Status == PaymentIntentStatus.AwaitingLandlord ||
             intent.Status == PaymentIntentStatus.Succeeded))
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Bạn đã báo đã chuyển khoản. Hóa đơn đang chờ chủ trọ xác nhận — vui lòng không thanh toán lại cho đến khi có kết quả.",
                invoiceId = invoice.InvoiceId
            });
        }

        if (intent.Status == PaymentIntentStatus.Cancelled || intent.Status == PaymentIntentStatus.Expired)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Yêu cầu thanh toán này không còn hiệu lực. Vui lòng tạo thanh toán mới từ trang hóa đơn.",
                invoiceId = invoice.InvoiceId
            });
        }

        if (intent.Provider == PaymentProviders.VIETQR && intent.Status != PaymentIntentStatus.Pending)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Không thể hiển thị mã thanh toán cho yêu cầu này.",
                invoiceId = invoice.InvoiceId
            });
        }

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
        //var bankCode = bank.BankCode.Trim().ToLower();

        var encodedContent = Uri.EscapeDataString(transferContent);
        //var qrImageUrl =
        //    $"https://img.vietqr.io/image/{bankCode}-{bank.BankAccountNumber}-qr_only.png?amount={amountVnd}&addInfo={encodedContent}";
        var qrImageUrl =
                        $"https://img.vietqr.io/image/{bank.BankCode.Trim().ToLower()}-{bank.BankAccountNumber}-compact2.png" +
                        $"?amount={amountVnd}&addInfo={Uri.EscapeDataString(transferContent)}";
        Console.WriteLine(qrImageUrl);
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
        var intent = await _paymentRepo.GetIntentAsync(paymentIntentId);
        if (intent == null)
            return NotFound();

        if (intent.Provider != PaymentProviders.CASH)
            return RedirectToAction(nameof(PaymentNotice), new { message = "Yêu cầu không hợp lệ.", invoiceId = intent.InvoiceId });

        var propLandlordId = intent.Invoice?.Room?.Property?.LandlordId ?? 0;
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action(nameof(CashConfirm), "Payment", new { paymentIntentId })
            });
        }

        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        if (landlordId == 0 || landlordId != propLandlordId)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Bạn không có quyền xác nhận thanh toán này.",
                invoiceId = intent.InvoiceId
            });
        }

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

    // POST: /Payment/CashConfirmReject — chủ trọ báo chưa nhận đủ tiền mặt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CashConfirmReject(int paymentIntentId)
    {
        var intent = await _paymentRepo.GetIntentAsync(paymentIntentId);
        if (intent == null)
            return NotFound();

        if (intent.Provider != PaymentProviders.CASH)
            return RedirectToAction(nameof(PaymentNotice), new { message = "Yêu cầu không hợp lệ.", invoiceId = intent.InvoiceId });

        var propLandlordId = intent.Invoice?.Room?.Property?.LandlordId ?? 0;
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action(nameof(CashConfirm), "Payment", new { paymentIntentId })
            });
        }

        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        if (landlordId == 0 || landlordId != propLandlordId)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Bạn không có quyền thao tác trên yêu cầu này.",
                invoiceId = intent.InvoiceId
            });
        }

        if (intent.Status != PaymentIntentStatus.Pending)
        {
            TempData["Info"] = "Yêu cầu tiền mặt này không còn ở trạng thái chờ xác nhận.";
            return RedirectToAction(nameof(InvoiceController.History), "Invoice");
        }

        if (intent.Payments.Any(p => p.Status == PaymentStatus.Succeeded))
        {
            return RedirectToAction(nameof(CashReceipt), new
            {
                paymentId = intent.Payments.First(p => p.Status == PaymentStatus.Succeeded).PaymentId
            });
        }

        var rejectPayment = new Payment
        {
            InvoiceId = intent.InvoiceId,
            PaymentIntentId = intent.PaymentIntentId,
            Provider = PaymentProviders.CASH,
            ProviderTxnId = $"CASH-REJECT-{DateTime.UtcNow:yyyyMMddHHmmss}-INTENT{intent.PaymentIntentId}",
            Amount = intent.Amount,
            PaidAt = DateTime.UtcNow,
            Status = PaymentStatus.Rejected,
            RawCallbackJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "cash_reject_landlord",
                rejectedAt = DateTime.UtcNow
            })
        };

        await _paymentRepo.AddPaymentAsync(rejectPayment);
        intent.Status = PaymentIntentStatus.Cancelled;
        if (intent.Invoice.Status == InvoiceStatus.Paid)
            intent.Invoice.Status = InvoiceStatus.Unpaid;

        await _paymentRepo.SaveChangesAsync();

        TempData["Success"] = "Đã ghi nhận: chưa nhận tiền mặt. Người thuê có thể tạo yêu cầu thanh toán lại.";
        return RedirectToAction(nameof(InvoiceController.History), "Invoice");
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
            Provider = payment.Provider ?? "",

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

        var noFinalPayment = !intent.Payments.Any(p =>
            p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Rejected);

        if (intent.Status == PaymentIntentStatus.AwaitingLandlord && noFinalPayment)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Bạn đã báo đã chuyển khoản. Hóa đơn đang chờ chủ trọ xác nhận.",
                invoiceId = intent.InvoiceId
            });
        }

        // Chuẩn hóa bản ghi cũ (succeeded nhưng chưa có Payment) → awaiting_landlord
        if (intent.Status == PaymentIntentStatus.Succeeded && noFinalPayment)
        {
            intent.Status = PaymentIntentStatus.AwaitingLandlord;
            await _paymentRepo.SaveChangesAsync();

            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Cảm ơn bạn. Thanh toán của bạn đang chờ chủ trọ xác nhận.",
                invoiceId = intent.InvoiceId
            });
        }

        if (intent.Status != PaymentIntentStatus.Pending)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Yêu cầu thanh toán không còn ở trạng thái có thể xác nhận.",
                invoiceId = intent.InvoiceId
            });
        }

        // Đánh dấu intent: người thuê đã báo đã chuyển khoản; chờ chủ trọ (không dùng succeeded — tránh tạo intent VietQR trùng).
        intent.Status = PaymentIntentStatus.AwaitingLandlord;
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

        var noFinal = !intent.Payments.Any(p =>
            p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Rejected);
        var canReject = intent.Status == PaymentIntentStatus.AwaitingLandlord
            || (intent.Status == PaymentIntentStatus.Succeeded && noFinal);
        if (!canReject)
        {
            return RedirectToAction(nameof(PaymentNotice), new
            {
                message = "Yêu cầu này không ở trạng thái chờ xác nhận.",
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

        intent.Status = PaymentIntentStatus.Cancelled;
        if (intent.Invoice.Status == InvoiceStatus.Paid)
            intent.Invoice.Status = InvoiceStatus.Unpaid;

        await _paymentRepo.SaveChangesAsync();

        return RedirectToAction(nameof(PaymentNotice), new
        {
            message = "Yêu cầu thanh toán đã bị từ chối. Người thuê có thể tạo thanh toán lại.",
            invoiceId = intent.InvoiceId
        });
    }

    // GET: /Payment/VietQrRequests — chuyển vào Cài đặt thanh toán (bookmark cũ vẫn hoạt động)
    [HttpGet]
    public IActionResult VietQrRequests()
    {
        return Redirect(Url.Action("Index", "PaymentSettings") + "#vietqr-requests");
    }
}
