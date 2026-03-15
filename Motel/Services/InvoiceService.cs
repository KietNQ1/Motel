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
    private readonly IFeeTypeRepository _feeTypeRepo;
    private readonly IFeeSettingRepository _feeSettingRepo;
    private readonly IMeterReadingRepository _meterRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IInvoiceLineRepository _lineRepo;
    private readonly IUnitOfWork _uow;
    private readonly MotelDbContext _db;

    public InvoiceService(
        IContractRepository contractRepo,
        IRoomRepository roomRepo,
        IFeeTypeRepository feeTypeRepo,
        IFeeSettingRepository feeSettingRepo,
        IMeterReadingRepository meterRepo,
        IInvoiceRepository invoiceRepo,
        IInvoiceLineRepository lineRepo,
        IUnitOfWork uow,
        MotelDbContext db)
    {
        _contractRepo = contractRepo;
        _roomRepo = roomRepo;
        _feeTypeRepo = feeTypeRepo;
        _feeSettingRepo = feeSettingRepo;
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
        var contract = await _contractRepo.GetActiveContractByRoomIdAsync(vm.RoomId, ct)
                      ?? throw new InvalidOperationException("Phòng này không có hợp đồng đang hoạt động.");

        // 2) Room tồn tại?
        var room = await _roomRepo.GetRoomByIdAsync(vm.RoomId, ct)
                   ?? throw new InvalidOperationException("Room không tồn tại.");

        if (await _invoiceRepo.ExistsAsync(contract.ContractId, vm.PeriodMonth, ct))
            throw new InvalidOperationException("Hoá đơn kỳ này đã tồn tại cho hợp đồng này.");

        var feeTypes = await _feeTypeRepo.GetAllAsync(ct);
        var rentFeeType = feeTypes.FirstOrDefault(f => f.Name == "Rent") ?? throw new InvalidOperationException("System missing Rent FeeType.");
        var otherFeeType = feeTypes.FirstOrDefault(f => f.Name == "Other");

        if (otherFeeType == null) {
            // Fallback just in case
            otherFeeType = rentFeeType;
        }

        var lines = new List<InvoiceLine>();

        // rent
        lines.Add(NewLine(rentFeeType.FeeTypeId, "Tiền phòng", 1m, room.RentPrice));

        foreach (var item in vm.FeeItems)
        {
            decimal qty = 1;
            decimal price = item.BaseAmount;
            
            if (item.CalculationMethod == "meter")
            {
                var oldR = item.PreviousReading ?? 0;
                var newR = item.CurrentReading ?? 0;
                qty = Math.Max(0, newR - oldR);
                price = item.UnitPrice;
            }
            else if (item.CalculationMethod == "per_person")
            {
                qty = item.Quantity ?? 1;
                price = item.BaseAmount;
            }
            else if (item.CalculationMethod == "per_room" || item.CalculationMethod == "fixed")
            {
                qty = 1;
                price = item.BaseAmount;
            }

            if (qty > 0 || price > 0)
            {
                lines.Add(NewLine(item.FeeTypeId, $"{item.FeeTypeName}", qty, price));
            }
        }

        // extra
        foreach (var x in vm.ExtraCharges.Where(x => x.Amount != 0))
        {
            var desc = string.IsNullOrWhiteSpace(x.Description) ? "Phát sinh" : x.Description.Trim();
            lines.Add(NewLine(otherFeeType.FeeTypeId, desc, 1m, x.Amount));
        }

        var total = lines.Sum(x => x.LineTotal ?? 0m);

        // 7) Transaction: insert invoice -> save để lấy InvoiceId -> insert lines -> update total (đã set) -> save
        int newInvoiceId = 0;

        await _uow.ExecuteInTransactionAsync(async token =>
        {
            var invoice = new Invoice
            {
                ContractId = contract.ContractId,
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
        var elecItem = vm.FeeItems.FirstOrDefault(f => f.FeeTypeName == "Electricity");
        var waterItem = vm.FeeItems.FirstOrDefault(f => f.FeeTypeName == "Water");

        if (elecItem != null && waterItem != null &&
            elecItem.PreviousReading.HasValue && elecItem.CurrentReading.HasValue &&
            waterItem.PreviousReading.HasValue && waterItem.CurrentReading.HasValue)
        {
            var contract2 = await _contractRepo.GetActiveContractByRoomIdAsync(vm.RoomId, ct);
            if (contract2 != null)
            {
                await _meterRepo.SaveMeterReadingAsync(
                    roomId:           contract2.RoomId,
                    periodMonth:      vm.PeriodMonth,
                    electricOld:      elecItem.PreviousReading.Value,
                    electricNew:      elecItem.CurrentReading.Value,
                    waterOld:         waterItem.PreviousReading.Value,
                    waterNew:         waterItem.CurrentReading.Value,
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

    // private async Task<(int eOld, int eNew, int wOld, int wNew)> ResolveMeterAsync ...
    // Removed because UI dynamically builds MeterReadings

    private static InvoiceLine NewLine(int feeTypeId, string desc, decimal qty, decimal unitPrice)
    {
        if (qty < 0) qty = 0;
        if (unitPrice < 0) unitPrice = 0;

        return new InvoiceLine
        {
            FeeTypeId = feeTypeId,
            Description = desc,
            Quantity = qty,
            UnitPrice = unitPrice,
            LineTotal = qty * unitPrice
        };
    }
}
