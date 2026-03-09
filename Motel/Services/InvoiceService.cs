using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interface;
using Motel.ViewModels.Invoice;

namespace Motel.Services;

public sealed class InvoiceService : IInvoiceService
{
    private readonly IContractRepository _contractRepo;
    private readonly IRoomRepository _roomRepo;
    private readonly IRoomUtilitySettingRepository _settingRepo;
    private readonly IMeterReadingRepository _meterRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IInvoiceLineRepository _lineRepo;
    private readonly IUnitOfWork _uow;
    private readonly MotelDbContext _db;

    public InvoiceService(
        IContractRepository contractRepo,
        IRoomRepository roomRepo,
        IRoomUtilitySettingRepository settingRepo,
        IMeterReadingRepository meterRepo,
        IInvoiceRepository invoiceRepo,
        IInvoiceLineRepository lineRepo,
        IUnitOfWork uow,
        MotelDbContext db)
    {
        _contractRepo = contractRepo;
        _roomRepo = roomRepo;
        _settingRepo = settingRepo;
        _meterRepo = meterRepo;
        _invoiceRepo = invoiceRepo;
        _lineRepo = lineRepo;
        _uow = uow;
        _db = db;
    }

    public async Task<int> CreateInvoiceAsync(CreateInvoiceViewModel vm, CancellationToken ct = default)
    {
        ValidatePeriodMonth(vm.PeriodMonth);

        // 1) Contract active?
        var contract = await _contractRepo.GetActiveByIdAsync(vm.ContractId, ct)
                      ?? throw new InvalidOperationException("Contract không tồn tại hoặc không còn active.");

        // 2) Room tồn tại?
        var room = await _roomRepo.GetRoomByIdAsync(contract.RoomId, ct)
                   ?? throw new InvalidOperationException("Room không tồn tại.");

        // 3) Chặn tạo trùng (ContractId + PeriodMonth)
        if (await _invoiceRepo.ExistsAsync(vm.ContractId, vm.PeriodMonth, ct))
            throw new InvalidOperationException("Hoá đơn kỳ này đã tồn tại cho hợp đồng này.");

        // 4) Load utility settings theo kỳ; nếu chưa có thì dùng mặc định (0) để vẫn tạo được hóa đơn
        var setting = await _settingRepo.GetEffectiveAsync(room.RoomId, vm.PeriodMonth, ct)
                      ?? new RoomUtilitySetting
                      {
                          ElectricUnitPrice = 0,
                          WaterUnitPrice = 0,
                          InternetFee = 0,
                          TrashFee = 0
                      };

        // 5) Meter readings: ưu tiên vm, không có thì lấy DB
        var (eOld, eNew, wOld, wNew) = await ResolveMeterAsync(vm, room.RoomId, ct);

        var electricUsed = Math.Max(0, eNew - eOld);
        var waterUsed = Math.Max(0, wNew - wOld);

        // 6) Build lines
        var lines = new List<InvoiceLine>();

        // rent
        lines.Add(NewLine("rent", "Tiền phòng", 1m, room.RentPrice));

        // electric
        if (electricUsed > 0)
        {
            lines.Add(NewLine(
                "electric",
                $"Tiền điện ({eOld} → {eNew})",
                electricUsed,
                vm.ElectricUnitPrice ?? setting.ElectricUnitPrice
            ));
        }

        // water: theo người nếu có, không thì theo đồng hồ
        if (vm.WaterPeopleCount.HasValue && vm.WaterPricePerPerson.HasValue
            && vm.WaterPeopleCount.Value > 0 && vm.WaterPricePerPerson.Value > 0)
        {
            lines.Add(NewLine(
                "water",
                $"Tiền nước theo người (x{vm.WaterPeopleCount.Value})",
                vm.WaterPeopleCount.Value,
                vm.WaterPricePerPerson.Value
            ));
        }
        else
        {
            if (waterUsed > 0)
            {
                lines.Add(NewLine(
                    "water",
                    $"Tiền nước ({wOld} → {wNew})",
                    waterUsed,
                    vm.WaterUnitPrice ?? setting.WaterUnitPrice
                ));
            }
        }

        // internet / trash (đúng schema sample của bạn)
        if (setting.InternetFee > 0)
            lines.Add(NewLine("internet", "Internet", 1m, setting.InternetFee));

        if (setting.TrashFee > 0)
            lines.Add(NewLine("trash", "Rác", 1m, setting.TrashFee));

        // extra
        foreach (var x in vm.ExtraCharges.Where(x => x.Amount != 0))
        {
            var type = string.IsNullOrWhiteSpace(x.ItemType) ? "other" : x.ItemType.Trim();
            var desc = string.IsNullOrWhiteSpace(x.Description) ? "Phát sinh" : x.Description.Trim();
            lines.Add(NewLine(type, desc, 1m, x.Amount));
        }

        var total = lines.Sum(x => x.LineTotal ?? 0m);

        // 7) Transaction: insert invoice -> save để lấy InvoiceId -> insert lines -> update total (đã set) -> save
        int newInvoiceId = 0;

        await _uow.ExecuteInTransactionAsync(async token =>
        {
            var invoice = new Invoice
            {
                ContractId = vm.ContractId,
                RoomId = room.RoomId,
                PeriodMonth = vm.PeriodMonth,
                TotalAmount = total,
                Status = "unpaid",
                DueDate = vm.DueDate,          // DateOnly (đúng lỗi bạn gặp trước đó)
                CreatedAt = DateTime.Now       // hoặc UtcNow tuỳ bạn
            };

            await _invoiceRepo.AddAsync(invoice, token);
            await _uow.SaveChangesAsync(token); // ✅ lấy InvoiceId

            foreach (var line in lines)
                line.InvoiceId = invoice.InvoiceId;

            await _lineRepo.AddRangeAsync(lines, token);
            await _uow.SaveChangesAsync(token);

            newInvoiceId = invoice.InvoiceId;
        }, ct);

        // 8) Lưu chỉ số điện/nước vào MeterReadings (UPSERT qua stored procedure)
        //    Chỉ lưu khi user nhập đủ 4 giá trị chỉ số
        if (vm.ElectricOld.HasValue && vm.ElectricNew.HasValue
            && vm.WaterOld.HasValue && vm.WaterNew.HasValue)
        {
            var contract2 = await _contractRepo.GetActiveByIdAsync(vm.ContractId, ct);
            if (contract2 != null)
            {
                await _meterRepo.SaveMeterReadingAsync(
                    roomId:           contract2.RoomId,
                    periodMonth:      vm.PeriodMonth,
                    electricOld:      vm.ElectricOld.Value,
                    electricNew:      vm.ElectricNew.Value,
                    waterOld:         vm.WaterOld.Value,
                    waterNew:         vm.WaterNew.Value,
                    recordedByUserId: 1,  // TODO: lấy từ claims sau khi có auth
                    ct:               ct
                );
            }
        }

        return newInvoiceId;
    }

    public Task<Invoice?> GetInvoiceWithLinesAsync(int invoiceId, CancellationToken ct = default)
        => _invoiceRepo.GetByIdWithLinesAsync(invoiceId, ct);

    public async Task<List<TransactionHistoryViewModel>> GetTransactionHistoryAsync(
        int landlordId, CancellationToken ct = default)
    {
        // Query từ view vw_TransactionHistory
        var rows = await _db.Database
            .SqlQueryRaw<TransactionHistoryRaw>(
                @"SELECT InvoiceId, PeriodMonth, PeriodLabel, TotalAmount, InvoiceStatus,
                         DueDate, InvoiceCreatedAt, RoomId, RoomName, RentPrice,
                         PropertyName, TenantName, TenantPhone, ContractId,
                         PaidAmount, PaidAt
                  FROM   dbo.vw_TransactionHistory
                  WHERE  LandlordId = {0}
                  ORDER  BY InvoiceCreatedAt DESC",
                landlordId)
            .ToListAsync(ct);

        return rows.Select(r => new TransactionHistoryViewModel
        {
            InvoiceId        = r.InvoiceId,
            PeriodMonth      = r.PeriodMonth,
            PeriodLabel      = r.PeriodLabel,
            TotalAmount      = r.TotalAmount,
            InvoiceStatus    = r.InvoiceStatus,
            DueDate          = r.DueDate,
            InvoiceCreatedAt = r.InvoiceCreatedAt,
            RoomId           = r.RoomId,
            RoomName         = r.RoomName,
            RentPrice        = r.RentPrice,
            PropertyName     = r.PropertyName,
            TenantName       = r.TenantName,
            TenantPhone      = r.TenantPhone,
            ContractId       = r.ContractId,
            PaidAmount       = r.PaidAmount,
            PaidAt           = r.PaidAt
        }).ToList();
    }

    // Raw DTO for SQL query mapping
    private sealed class TransactionHistoryRaw
    {
        public int InvoiceId { get; set; }
        public int PeriodMonth { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string InvoiceStatus { get; set; } = string.Empty;
        public DateOnly DueDate { get; set; }
        public DateTime InvoiceCreatedAt { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public decimal RentPrice { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string? TenantPhone { get; set; }
        public int ContractId { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    // ----------------- helpers -----------------

    private static void ValidatePeriodMonth(int yyyymm)
    {
        var year = yyyymm / 100;
        var month = yyyymm % 100;
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            throw new ArgumentException("PeriodMonth không hợp lệ (YYYYMM).");
    }

    private async Task<(int eOld, int eNew, int wOld, int wNew)> ResolveMeterAsync(
        CreateInvoiceViewModel vm, int roomId, CancellationToken ct)
    {
        if (vm.ElectricOld.HasValue && vm.ElectricNew.HasValue && vm.WaterOld.HasValue && vm.WaterNew.HasValue)
            return (vm.ElectricOld.Value, vm.ElectricNew.Value, vm.WaterOld.Value, vm.WaterNew.Value);

        var mr = await _meterRepo.GetByRoomAndPeriodAsync(roomId, vm.PeriodMonth, ct);
        if (mr is null)
            throw new InvalidOperationException("Chưa có MeterReadings cho phòng/kỳ này (hoặc bạn chưa nhập chỉ số).");

        return (mr.ElectricOld, mr.ElectricNew, mr.WaterOld, mr.WaterNew);
    }

    private static InvoiceLine NewLine(string itemType, string desc, decimal qty, decimal unitPrice)
    {
        if (qty < 0) qty = 0;
        if (unitPrice < 0) unitPrice = 0;

        return new InvoiceLine
        {
            ItemType = itemType,
            Description = desc,
            Quantity = qty,
            UnitPrice = unitPrice,
            LineTotal = qty * unitPrice
        };
    }
}
