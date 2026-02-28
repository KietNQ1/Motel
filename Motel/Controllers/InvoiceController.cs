using Microsoft.AspNetCore.Mvc;
using Motel.Services.Interface;
using Motel.ViewModels.Invoice;

namespace Motel.Controllers;

public sealed class InvoiceController : Controller
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    // GET: /Invoice/Create?contractId=1&periodMonth=202602
    [HttpGet]
    public IActionResult Create(int contractId, int? periodMonth)
    {
        var now = DateTime.Now;
        var yyyymm = periodMonth ?? (now.Year * 100 + now.Month);

        var vm = new CreateInvoiceViewModel
        {
            ContractId = contractId,
            PeriodMonth = yyyymm,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        };

        return View(vm); // => Views/Invoice/Create.cshtml
    }

    // POST: /Invoice/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInvoiceViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            foreach (var e in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine(e.ErrorMessage);
            }
            return View(vm);
        }

        try
        {
            var invoiceId = await _invoiceService.CreateInvoiceAsync(vm, ct);
            return RedirectToAction(nameof(Details), new { id = invoiceId }); // => /Invoice/Details/5
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    // GET: /Invoice/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var invoice = await _invoiceService.GetInvoiceWithLinesAsync(id, ct);
        if (invoice is null) return NotFound();

        return View(invoice); // => Views/Invoice/Details.cshtml
    }
}
