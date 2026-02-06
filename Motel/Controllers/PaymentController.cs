using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Motel.Repositories.Interface;
using Motel.Services;
using Motel.Services.Interface;
using Motel.ViewModels.Payments;

namespace Motel.Controllers;

public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepo;

    public PaymentsController(IPaymentService paymentService, IPaymentRepository paymentRepo)
    {
        _paymentService = paymentService;
        _paymentRepo = paymentRepo;
    }

    // GET: /Payments/CreateIntent?invoiceId=1 (optional)
    [HttpGet]
    public async Task<IActionResult> CreateIntent(int? invoiceId = null)
    {
        // TODO: bạn có thể lọc invoice theo landlord/tenant tuỳ dự án.
        // Ở đây demo đơn giản: lấy 1 invoice nếu có invoiceId

        var vm = new CreatePaymentIntentViewModel();

        // Bạn cần tự build danh sách InvoiceOptions.
        // Nếu repo bạn chưa có hàm list invoice, tạm lấy 1 invoice theo id để demo.
        if (invoiceId.HasValue)
        {
            var inv = await _paymentRepo.GetInvoiceAsync(invoiceId.Value);
            if (inv != null)
            {
                vm.InvoiceId = inv.InvoiceId;
                // TODO: đổi đúng field tổng tiền của Invoice bạn
                vm.Amount = inv.TotalAmount;

                vm.InvoiceOptions.Add(new SelectListItem
                {
                    Value = inv.InvoiceId.ToString(),
                    Text = $"Invoice #{inv.InvoiceId} - {vm.Amount:n0} VND",
                    Selected = true
                });
            }
        }
        else
        {
            // Nếu bạn chưa có list invoice, cứ để 1 option placeholder để khỏi lỗi UI
            vm.InvoiceOptions.Add(new SelectListItem { Value = "", Text = "Choose an invoice..." });
        }

        return View(vm);
    }

    // POST: /Payments/CreateIntent
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateIntent(CreatePaymentIntentViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var intent = await _paymentService.CreateIntentAsync(vm.InvoiceId, vm.Provider);

        // Điều hướng theo provider
        if (vm.Provider == PaymentProviders.CASH)
        {
            // Sang màn xác nhận cash (landlord)
            return RedirectToAction(nameof(CashConfirm), new { paymentIntentId = intent.PaymentIntentId });
        }

        // Online: tạm thời bạn chưa implement gateway => trả về trang "chưa hỗ trợ"
        // Sau này PAYOS/VNPAY/MOMO: tạo link/QR rồi redirect/show QR
        return RedirectToAction(nameof(IntentCreated), new { id = intent.PaymentIntentId });
    }

    // GET: /Payments/IntentCreated/5
    [HttpGet]
    public IActionResult IntentCreated(int id)
    {
        ViewBag.IntentId = id;
        return View();
    }

    // GET: /Payments/CashConfirm?paymentIntentId=5
    [HttpGet]
    public IActionResult CashConfirm(int paymentIntentId)
    {
        ViewBag.PaymentIntentId = paymentIntentId;
        return View();
    }


    // POST: /Payments/CashConfirm
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

    // GET: /Payments/CashReceipt?paymentId=5
    [HttpGet]
    public async Task<IActionResult> CashReceipt(int paymentId)
    {
        // LẤY DATA ĐỂ HIỂN THỊ
        var payment = await _paymentRepo.GetPaymentForReceiptAsync(paymentId);
        if (payment == null) return NotFound();

        var vm = new CashReceiptViewModel
        {
            PaymentId = payment.PaymentId,
            ProviderTxnId = payment.ProviderTxnId,
            PaidAt = payment.PaidAt,

            TenantName = payment.Invoice.Contract.Tenant.FullName,
            TenantPhone = payment.Invoice.Contract.Tenant.Phone,

            RoomName = payment.Invoice.Contract.Room.RoomName,
            PropertyName = payment.Invoice.Contract.Room.Property.Name,

            TotalAmount = payment.Amount
        };

        return View(vm);
    }
}
