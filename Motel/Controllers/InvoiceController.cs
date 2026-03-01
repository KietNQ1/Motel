using Microsoft.AspNetCore.Mvc;
using Motel.Repositories.Interface;
using Motel.Services.Interface;
using Motel.ViewModels.Invoice;

namespace Motel.Controllers;

public sealed class InvoiceController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly IContractRepository _contractRepo;
    private readonly IRoomRepository _roomRepo;
    private readonly IMeterReadingRepository _meterRepo;

    public InvoiceController(
        IInvoiceService invoiceService,
        IContractRepository contractRepo,
        IRoomRepository roomRepo,
        IMeterReadingRepository meterRepo)
    {
        _invoiceService = invoiceService;
        _contractRepo = contractRepo;
        _roomRepo = roomRepo;
        _meterRepo = meterRepo;
    }

    // GET: /Invoice/Create?contractId=1&periodMonth=202602
    [HttpGet]
    public async Task<IActionResult> Create(int contractId, int? periodMonth, CancellationToken ct)
    {
        var now = DateTime.Now;
        var yyyymm = periodMonth ?? (now.Year * 100 + now.Month);

        var vm = new CreateInvoiceViewModel
        {
            ContractId = contractId,
            PeriodMonth = yyyymm,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        };

        var contract = await _contractRepo.GetActiveByIdAsync(contractId, ct);
        if (contract != null)
        {
            var room = await _roomRepo.GetRoomByIdAsync(contract.RoomId, ct);
            if (room != null)
            {
                vm.RoomName = room.RoomName;
            }

            var meter = await _meterRepo.GetByRoomAndPeriodAsync(contract.RoomId, yyyymm, ct);
            if (meter != null)
            {
                vm.ElectricOld = meter.ElectricOld;
                vm.ElectricNew = meter.ElectricNew;
                vm.WaterOld = meter.WaterOld;
                vm.WaterNew = meter.WaterNew;
            }
            else
            {
                var prevPeriod = yyyymm % 100 == 1 ? yyyymm - 89 : yyyymm - 1;
                var prevMeter = await _meterRepo.GetByRoomAndPeriodAsync(contract.RoomId, prevPeriod, ct);
                if (prevMeter != null)
                {
                    vm.ElectricOld = prevMeter.ElectricNew;
                    vm.WaterOld = prevMeter.WaterNew;
                }
            }
        }

        return View(vm);
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
