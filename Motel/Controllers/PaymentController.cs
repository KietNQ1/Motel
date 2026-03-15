using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Motel.Repositories.Interface;
using Motel.Services;
using Motel.Services.Interface;
using Motel.ViewModels.Payment;
using static Motel.ViewModels.Payment.CashReceiptViewModel;

namespace Motel.Controllers;

public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IInvoiceRepository _invoiceRepo; // ✅ thêm

    public PaymentController(
        IPaymentService paymentService,
        IPaymentRepository paymentRepo,
        IInvoiceRepository invoiceRepo)
    {
        _paymentService = paymentService;
        _paymentRepo = paymentRepo;
        _invoiceRepo = invoiceRepo;
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

            // Online: tạm thời bạn chưa implement gateway => trả về trang "chưa hỗ trợ"
            // Sau này PAYOS/VNPAY/MOMO: tạo link/QR rồi redirect/show QR
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
}
