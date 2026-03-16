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
    private readonly IFeeTypeRepository _feeTypeRepo;
    private readonly IFeeSettingRepository _feeSettingRepo;

    public InvoiceController(
        IInvoiceService invoiceService,
        IContractRepository contractRepo,
        IRoomRepository roomRepo,
        IMeterReadingRepository meterRepo,
        IFeeTypeRepository feeTypeRepo,
        IFeeSettingRepository feeSettingRepo)
    {
        _invoiceService = invoiceService;
        _contractRepo = contractRepo;
        _roomRepo = roomRepo;
        _meterRepo = meterRepo;
        _feeTypeRepo = feeTypeRepo;
        _feeSettingRepo = feeSettingRepo;
    }

    // GET: /Invoice/History
    [HttpGet]
    public async Task<IActionResult> History(CancellationToken ct)
    {
        // TODO: lấy landlordId từ claims sau khi có auth, tạm dùng 1
        var landlordId = 1;
        var history = await _invoiceService.GetTransactionHistoryAsync(landlordId, ct);
        return View(history);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int roomId, int? periodMonth, CancellationToken ct)
    {
        var now = DateTime.Now;
        var yyyymm = periodMonth ?? (now.Year * 100 + now.Month);

        var contract = await _contractRepo.GetActiveContractByRoomIdAsync(roomId, ct);
        if (contract == null)
        {
            return Content("Phòng này hiện tại chưa có hợp đồng thuê nào đang hoạt động.");
        }

        var vm = new CreateInvoiceViewModel
        {
            ContractId = contract.ContractId,
            RoomId = roomId,
            PeriodMonth = yyyymm,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        };

        var room = await _roomRepo.GetRoomByIdAsync(roomId, ct);
        if (room != null)
        {
            vm.RoomName = room.RoomName;
            var occupancies = await _contractRepo.GetActiveOccupanciesAsync(roomId, ct);
            int occupantCount = occupancies.Count;

            var feeSettings = await _feeSettingRepo.GetEffectiveForRoomAsync(room.PropertyId, room.RoomId, yyyymm, ct);
            var prevPeriod = yyyymm % 100 == 1 ? (yyyymm - 100) + 11 : yyyymm - 1;
            
            var currentMeter = await _meterRepo.GetByRoomAndPeriodAsync(roomId, yyyymm, ct);
            var prevMeter = await _meterRepo.GetByRoomAndPeriodAsync(roomId, prevPeriod, ct);
            
            foreach (var setting in feeSettings)
                {
                    var feeItem = new InvoiceFeeItemVm
                    {
                        FeeTypeId = setting.FeeTypeId,
                        FeeTypeName = setting.FeeType?.Name ?? "Unknown",
                        CalculationMethod = setting.CalculationMethod,
                        UnitPrice = setting.UnitPrice,
                        BaseAmount = setting.BaseAmount
                    };

                    if (setting.CalculationMethod == "meter")
                    {
                        if (setting.FeeType?.Name == "Electricity") 
                        {
                            feeItem.CurrentReading = currentMeter?.ElectricNew;
                            feeItem.PreviousReading = currentMeter != null ? currentMeter.ElectricOld : prevMeter?.ElectricNew;
                        }
                        else if (setting.FeeType?.Name == "Water") 
                        {
                            feeItem.CurrentReading = currentMeter?.WaterNew;
                            feeItem.PreviousReading = currentMeter != null ? currentMeter.WaterOld : prevMeter?.WaterNew;
                        }
                    }
                    else if (setting.CalculationMethod == "per_person")
                    {
                        feeItem.Quantity = occupantCount > 0 ? occupantCount : 1;
                    }
                    
                    vm.FeeItems.Add(feeItem);
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
