using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Motel.Hubs;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interface;
using Motel.Services.Interfaces;
using Motel.ViewModels.Chat;

namespace Motel.Services;

public class ChatService : IChatService {
    private readonly IChatRepository _repo;
    private readonly IGeminiService _gemini;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IContractRepository _contractRepo;
    private readonly IRoomRepository _roomRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly ITaxRepository _taxRepo;
    private readonly IMeterReadingRepository _meterRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly IRoomFurnitureRepository _roomFurnitureRepo;
    private readonly IFeeTypeRepository _feeTypeRepo;
    private readonly IFeeSettingRepository _feeSettingRepo;
    private readonly IInvoiceLineRepository _invoiceLineRepo;



    public ChatService(
        IChatRepository repo,
        IGeminiService gemini,
        IHubContext<ChatHub> hubContext,
        IContractRepository contractRepo,
        IRoomRepository roomRepo,
        IInvoiceRepository invoiceRepo,
        ITransactionRepository transactionRepo,
        INotificationRepository notificationRepo,
        ITaxRepository taxRepo,
        IMeterReadingRepository meterRepo,
        ITenantRepository tenantRepo,
        IPaymentRepository paymentRepo,
        IPropertyRepository propertyRepo,
        IRoomFurnitureRepository roomFurnitureRepo,
        IFeeTypeRepository feeTypeRepo,
        IFeeSettingRepository feeSettingRepo,
        IInvoiceLineRepository invoiceLineRepo)
    {
        _repo = repo;
        _gemini = gemini;
        _hubContext = hubContext;
        _contractRepo = contractRepo;
        _roomRepo = roomRepo;
        _invoiceRepo = invoiceRepo;
        _transactionRepo = transactionRepo;
        _notificationRepo = notificationRepo;
        _taxRepo = taxRepo;
        _meterRepo = meterRepo;
        _tenantRepo = tenantRepo;
        _paymentRepo = paymentRepo;
        _propertyRepo = propertyRepo;
        _roomFurnitureRepo = roomFurnitureRepo;
        _feeTypeRepo = feeTypeRepo;
        _feeSettingRepo = feeSettingRepo;
        _invoiceLineRepo = invoiceLineRepo;
    }

    public async Task SendMessageAsync(int landlordId, int userId, string message, string connectionId) {
        // Lưu và gửi tin nhắn user về tất cả tab
        Console.WriteLine($"[ChatService] landlordId truyền vào: {landlordId}, userId realtime: {userId}");
        var userMsg = new ChatMessage { UserId = userId, Role = "user", Content = message };
        await _repo.SaveMessageAsync(userMsg);
        Console.WriteLine($"[ChatService] Gửi realtime tới userId: {userId} (UserIdentifier SignalR)");
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReceiveUserMessage", userMsg.Content);

        // Phân loại câu hỏi: nếu liên quan hợp đồng thì truy vấn dữ liệu thật
        string reply;
        var businessType = DetectBusinessType(message);
        switch (businessType)
        {
            case "contract":
                Console.WriteLine($"[ChatService] HandleContract truyền vào landlordId: {landlordId}");
                reply = await HandleContract(landlordId, message);
                break;
            case "room":
                Console.WriteLine($"[ChatService] HandleRoom truyền vào landlordId: {landlordId}");
                reply = await HandleRoom(landlordId, message);
                break;
            case "invoice":
                Console.WriteLine($"[ChatService] HandleInvoice truyền vào landlordId: {landlordId}");
                reply = await HandleInvoice(landlordId, message);
                break;
            case "transaction":
                Console.WriteLine($"[ChatService] HandleTransaction truyền vào landlordId: {landlordId}");
                reply = await HandleTransaction(landlordId, message);
                break;
            case "notification":
                Console.WriteLine($"[ChatService] HandleNotification truyền vào landlordId: {landlordId}");
                reply = await HandleNotification(landlordId, message);
                break;
            case "tax":
                Console.WriteLine($"[ChatService] HandleTax truyền vào landlordId: {landlordId}");
                reply = await HandleTax(landlordId, message);
                break;
            case "meter":
                Console.WriteLine($"[ChatService] HandleMeter truyền vào landlordId: {landlordId}");
                reply = await HandleMeter(landlordId, message);
                break;
            case "tenant":
                Console.WriteLine($"[ChatService] HandleTenant truyền vào landlordId: {landlordId}");
                reply = await HandleTenant(landlordId, message);
                break;
            case "payment-intent":
                Console.WriteLine($"[ChatService] HandlePaymentIntent truyền vào landlordId: {landlordId}");
                reply = await HandlePaymentIntent(landlordId, message);
                break;
            case "payment":
                Console.WriteLine($"[ChatService] HandlePayment truyền vào landlordId: {landlordId}");
                reply = await HandlePayment(landlordId, message);
                break;
            case "property":
                Console.WriteLine($"[ChatService] HandleProperty truyền vào landlordId: {landlordId}");
                reply = await HandleProperty(landlordId, message);
                break;
            case "room-furniture":
                Console.WriteLine($"[ChatService] HandleRoomFurniture truyền vào landlordId: {landlordId}");
                reply = await HandleRoomFurniture(landlordId, message);
                break;
            case "room-occupancy":
                Console.WriteLine($"[ChatService] HandleRoomOccupancy truyền vào landlordId: {landlordId}");
                reply = await HandleRoomOccupancy(landlordId, message);
                break;
            case "fee-type":
                Console.WriteLine($"[ChatService] HandleFeeType truyền vào landlordId: {landlordId}");
                reply = await HandleFeeType(landlordId, message);
                break;
            case "fee-setting":
                Console.WriteLine($"[ChatService] HandleFeeSetting truyền vào landlordId: {landlordId}");
                reply = await HandleFeeSetting(landlordId, message);
                break;
            case "invoice-line":
                Console.WriteLine($"[ChatService] HandleInvoiceLine truyền vào landlordId: {landlordId}");
                reply = await HandleInvoiceLine(landlordId, message);
                break;
            case "stored-file-reference":
                Console.WriteLine($"[ChatService] HandleStoredFileReference truyền vào landlordId: {landlordId}");
                reply = await HandleStoredFileReference(landlordId, message);
                break;
            case "stored-file":
                Console.WriteLine($"[ChatService] HandleStoredFile truyền vào landlordId: {landlordId}");
                reply = await HandleStoredFile(landlordId, message);
                break;
            case "subscription":
                Console.WriteLine($"[ChatService] HandleSubscription truyền vào landlordId: {landlordId}");
                reply = await HandleSubscription(landlordId, message);
                break;
            case "bank-account":
                Console.WriteLine($"[ChatService] HandleBankAccount truyền vào landlordId: {landlordId}");
                reply = await HandleBankAccount(landlordId, message);
                break;
            case "furniture-status":
                Console.WriteLine($"[ChatService] HandleFurnitureStatus truyền vào landlordId: {landlordId}");
                reply = await HandleFurnitureStatus(landlordId, message);
                break;
            case "furniture-catalog":
                Console.WriteLine($"[ChatService] HandleFurnitureCatalog truyền vào landlordId: {landlordId}");
                reply = await HandleFurnitureCatalog(landlordId, message);
                break;
            case "landlord":
                Console.WriteLine($"[ChatService] HandleLandlord truyền vào landlordId: {landlordId}");
                reply = await HandleLandlord(landlordId, message);
                break;
            case "guide":
                Console.WriteLine($"[ChatService] HandleGuide truyền vào landlordId: {landlordId}");
                reply = await HandleGuide(landlordId, message);
                break;
            default:
                var history = await _repo.GetRecentMessagesAsync(userId);
                reply = await _gemini.GenerateReplyAsync(history, message);
                break;
        }
        await _repo.SaveMessageAsync(new ChatMessage { UserId = userId, Role = "model", Content = reply });
        Console.WriteLine($"[ChatService] Gửi realtime bot tới userId: {userId} (UserIdentifier SignalR)");
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveBotMessage", reply);
    }

    // Phân loại nghiệp vụ
    private string DetectBusinessType(string message)
    {
        var lower = message.ToLower();

        // Ưu tiên guide để các câu như "hướng dẫn phòng" không bị rơi vào case room/invoice.
        var guideKeywords = new[] { "hướng dẫn", "guide", "cách sử dụng", "làm sao", "thao tác", "help" };
        if (guideKeywords.Any(k => lower.Contains(k)))
        {
            return "guide";
        }

        var map = new Dictionary<string, string[]>
        {
            { "contract", new[] { "hợp đồng", "contract", "ký hợp đồng", "xem hợp đồng" } },
            { "room", new[] { "phòng", "room", "danh sách phòng", "tình trạng phòng" } },
            { "invoice", new[] { "hóa đơn", "invoice", "tiền điện", "tiền nước", "thanh toán" } },
            { "transaction", new[] { "giao dịch", "transaction", "chuyển khoản", "lịch sử thanh toán" } },
            { "notification", new[] { "thông báo", "notification", "nhắc nhở" } },
            { "tax", new[] { "thuế", "tax", "ước tính thuế", "quy định thuế" } },
            { "meter", new[] { "chỉ số điện", "meter", "điện nước", "lịch sử điện nước" } },
            { "tenant", new[] { "người thuê", "tenant", "danh sách người thuê" } },
            { "payment-intent", new[] { "payment intent", "yêu cầu thanh toán", "intent", "chờ thanh toán" } },
            { "payment", new[] { "payment", "thanh toán online", "vietqr", "cash", "phiếu thu" } },
            { "property", new[] { "property", "bất động sản", "nhà trọ", "khu trọ", "toà nhà" } },
            { "room-furniture", new[] { "nội thất", "furniture", "đồ đạc", "trang bị phòng" } },
            { "room-occupancy", new[] { "ở ghép", "occupancy", "đang ở", "chuyển vào", "chuyển ra" } },
            { "fee-type", new[] { "loại phí", "fee type", "danh mục phí" } },
            { "fee-setting", new[] { "thiết lập phí", "fee setting", "đơn giá", "mức phí" } },
            { "invoice-line", new[] { "chi tiết hóa đơn", "invoice line", "dòng hóa đơn" } },
            { "stored-file-reference", new[] { "file reference", "liên kết tệp", "tham chiếu tệp", "ref file" } },
            { "stored-file", new[] { "tệp", "file", "hồ sơ", "đính kèm", "ảnh" } },
            { "subscription", new[] { "gói", "subscription", "plan", "gia hạn" } },
            { "bank-account", new[] { "tài khoản ngân hàng", "bank account", "stk", "ngân hàng" } },
            { "furniture-status", new[] { "trạng thái nội thất", "furniture status", "tình trạng nội thất" } },
            { "furniture-catalog", new[] { "danh mục nội thất", "furniture catalog", "trạng thái nội thất" } },
            { "landlord", new[] { "chủ trọ", "landlord", "thông tin chủ" } },
            { "guide", new[] { "hướng dẫn", "guide", "cách sử dụng", "làm sao", "thao tác", "help" } },
        };

        foreach (var kv in map)
            if (kv.Value.Any(k => lower.Contains(k))) return kv.Key;
        return "other";
    }

    // Xử lý từng nghiệp vụ (có thể mở rộng, tách file riêng nếu lớn)
    private async Task<string> HandleContract(int userId, string message)
    {
        var contracts = await _contractRepo.GetContractsByLandlordAsync(userId, default);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeCount = contracts.Count(c => c.Status == "active");
        var endingSoonCount = contracts.Count(c => c.Status == "active" && c.EndDate >= today && c.EndDate <= today.AddDays(30));
        var sample = contracts
            .OrderBy(c => c.EndDate)
            .Take(3)
            .Select(c => $"{c.Room.RoomName} - {c.Tenant.FullName} - {c.Status} - hết hạn {c.EndDate:dd/MM/yyyy}");

        var info = contracts.Count == 0
            ? "Bạn chưa có hợp đồng nào trong hệ thống."
            : $"Bạn có {contracts.Count} hợp đồng, đang active: {activeCount}, sắp hết hạn 30 ngày: {endingSoonCount}. " +
              $"Mẫu hợp đồng: {string.Join("; ", sample)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleRoom(int userId, string message)
    {
        var rooms = await _roomRepo.GetRoomsByLandlordAsync(userId);
        var occupied = rooms.Count(r => r.Status == "occupied");
        var available = rooms.Count(r => r.Status == "available");
        var maintenance = rooms.Count(r => r.Status == "maintenance");
        var avgRent = rooms.Count == 0 ? 0 : rooms.Average(r => r.RentPrice);
        var topRent = rooms.OrderByDescending(r => r.RentPrice).Take(3).Select(r => $"{r.RoomName}: {r.RentPrice:N0} VND");
        var info = rooms.Count == 0
            ? "Bạn chưa có phòng nào trong hệ thống."
            : $"Bạn có {rooms.Count} phòng (đang ở: {occupied}, trống: {available}, bảo trì: {maintenance}). " +
              $"Giá thuê trung bình: {avgRent:N0} VND. Phòng giá cao: {string.Join("; ", topRent)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleInvoice(int userId, string message)
    {
        var invoices = await _invoiceRepo.GetInvoicesByLandlordAsync(userId);
        var paidCount = invoices.Count(i => i.Status == "paid");
        var unpaidCount = invoices.Count(i => i.Status == "unpaid");
        var overdueCount = invoices.Count(i => i.Status != "paid" && i.DueDate < DateOnly.FromDateTime(DateTime.Today));
        var latestPeriods = invoices
            .OrderByDescending(i => i.PeriodMonth)
            .Take(3)
            .Select(i => i.PeriodMonth)
            .Distinct();
        var info = invoices.Count == 0
            ? "Bạn chưa có hóa đơn nào trong hệ thống."
            : $"Bạn có {invoices.Count} hóa đơn, tổng tiền: {invoices.Sum(i => i.TotalAmount):N0} VND. " +
              $"Đã thanh toán: {paidCount}, chưa thanh toán: {unpaidCount}, quá hạn: {overdueCount}. " +
              $"Kỳ gần nhất: {string.Join(", ", latestPeriods)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleTransaction(int userId, string message)
    {
        var trans = await _transactionRepo.GetTransactionsByUserIdAsync(userId);
        var inbound = trans.Where(t => t.Direction == "in").Sum(t => t.Amount);
        var outbound = trans.Where(t => t.Direction == "out").Sum(t => t.Amount);
        var byType = trans.GroupBy(t => t.Type)
            .Select(g => $"{g.Key}: {g.Count()} giao dịch, {g.Sum(x => x.Amount):N0} VND")
            .Take(3);
        var info = trans.Count == 0
            ? "Bạn chưa có giao dịch nào."
            : $"Bạn có {trans.Count} giao dịch, tổng tiền: {trans.Sum(t => t.Amount):N0} VND. " +
              $"Thu vào: {inbound:N0} VND, chi ra: {outbound:N0} VND. " +
              $"Nhóm giao dịch chính: {string.Join("; ", byType)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleNotification(int userId, string message)
    {
        var noti = await _notificationRepo.GetNotificationsByUserIdAsync(userId);
        var unread = noti.Count(n => n.ReadAt == null);
        var sent = noti.Count(n => n.SentAt != null);
        var channels = noti.GroupBy(n => n.Channel).Select(g => $"{g.Key}: {g.Count()}");
        var info = noti.Count == 0
            ? "Bạn chưa có thông báo nào."
            : $"Bạn có {noti.Count} thông báo, chưa đọc: {unread}, đã gửi: {sent}. " +
              $"Theo kênh: {string.Join("; ", channels)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleTax(int userId, string message)
    {
        var taxes = await _taxRepo.GetTaxEstimationsByUserIdAsync(userId);
        var latest = taxes.OrderByDescending(t => t.Year).FirstOrDefault();
        var info = taxes.Count == 0
            ? "Bạn chưa có dữ liệu thuế nào."
            : $"Bạn có {taxes.Count} bản ước tính thuế, tổng thuế đã ước tính: {taxes.Sum(t => t.TotalTaxAmount):N0} VND. " +
              $"Miễn thuế: {taxes.Count(t => t.IsExempt)} bản. " +
              (latest == null ? string.Empty : $"Năm gần nhất {latest.Year}: {latest.TotalTaxAmount:N0} VND.");
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleMeter(int userId, string message)
    {
        var meters = await _meterRepo.GetMeterReadingsByUserIdAsync(userId);
        var electricUsage = meters.Sum(m => Math.Max(0, m.ElectricNew - m.ElectricOld));
        var waterUsage = meters.Sum(m => Math.Max(0, m.WaterNew - m.WaterOld));
        var latestPeriod = meters.OrderByDescending(m => m.PeriodMonth).FirstOrDefault()?.PeriodMonth;
        var info = meters.Count == 0
            ? "Bạn chưa có chỉ số điện nước nào."
            : $"Bạn có {meters.Count} bản ghi chỉ số điện nước. " +
              $"Tổng tiêu thụ điện: {electricUsage}, nước: {waterUsage}. " +
              (latestPeriod.HasValue ? $"Kỳ mới nhất: {latestPeriod.Value}." : string.Empty);
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleTenant(int userId, string message)
    {
        var tenants = await _tenantRepo.GetTenantsByLandlordAsync(userId);
        var noPhone = tenants.Count(t => string.IsNullOrWhiteSpace(t.Phone));
        var noIdentity = tenants.Count(t => string.IsNullOrWhiteSpace(t.IdentityNo));
        var newest = tenants.OrderByDescending(t => t.CreatedAt).Take(3).Select(t => t.FullName);
        var info = tenants.Count == 0
            ? "Bạn chưa có người thuê nào."
            : $"Bạn có {tenants.Count} người thuê. Thiếu SĐT: {noPhone}, thiếu CCCD: {noIdentity}. " +
              $"Người thuê mới thêm: {string.Join(", ", newest)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandlePayment(int landlordId, string message)
    {
        var insight = await _paymentRepo.GetPaymentInsightByLandlordAsync(landlordId);

        var info = insight.TotalPayments == 0
            ? "Bạn chưa có giao dịch thanh toán đã ghi nhận."
            : $"Bạn có {insight.TotalPayments} giao dịch thanh toán, tổng tiền {insight.TotalAmount:N0} VND. " +
              $"Thành công: {insight.SucceededCount}, đang chờ: {insight.PendingCount}, lỗi/từ chối: {insight.FailedCount}. " +
              $"Mẫu gần nhất: {string.Join("; ", insight.RecentPaymentSamples)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandlePaymentIntent(int landlordId, string message)
    {
        var pendingCounts = await _paymentRepo.GetPendingIntentCountsByLandlordAsync(landlordId);
        var insight = await _paymentRepo.GetPaymentIntentInsightByLandlordAsync(landlordId);
        var info = insight.TotalIntents == 0
            ? "Bạn chưa có payment intent nào."
            : $"Bạn có {insight.TotalIntents} payment intent. Pending: {insight.PendingIntents}, chờ chủ trọ: {insight.AwaitingLandlordIntents}, đã hết hạn: {insight.ExpiredIntents}. " +
              $"Theo kênh pending: VietQR {pendingCounts.VietQrPendingCount}, Cash {pendingCounts.CashPendingCount}. " +
              $"Mẫu gần nhất: {string.Join("; ", insight.RecentIntentSamples)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleProperty(int landlordId, string message)
    {
        var properties = await _propertyRepo.GetPropertiesByLandlordIdAsync(landlordId);
        var totalRooms = properties.Sum(p => p.TotalRooms);
        var occupiedRooms = properties.Sum(p => p.OccupiedRooms);
        var availableRooms = properties.Sum(p => p.AvailableRooms);
        var occupancyRate = totalRooms == 0 ? 0 : (decimal)occupiedRooms * 100 / totalRooms;
        var top = properties.OrderByDescending(p => p.OccupiedRooms).Take(3).Select(p => $"{p.Name}: {p.OccupiedRooms}/{p.TotalRooms}");
        var info = properties.Count == 0
            ? "Bạn chưa có khu trọ/bất động sản nào trong hệ thống."
            : $"Bạn có {properties.Count} bất động sản, tổng {totalRooms} phòng (đang ở {occupiedRooms}, trống {availableRooms}), lấp đầy {occupancyRate:N1}%. " +
              $"Top công suất: {string.Join("; ", top)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleRoomFurniture(int landlordId, string message)
    {
        var furnitures = await _roomFurnitureRepo.GetRoomFurnituresByLandlordAsync(landlordId);
        var byStatus = furnitures.GroupBy(f => f.FurnitureStatus?.Name ?? "unknown")
            .Select(g => $"{g.Key}: {g.Count()} món")
            .Take(3);
        var byCatalog = furnitures.GroupBy(f => f.FurnitureCatalog?.Name ?? "unknown")
            .Select(g => $"{g.Key}: {g.Sum(x => x.Quantity)}")
            .OrderByDescending(x => x)
            .Take(3);

        var info = furnitures.Count == 0
            ? "Bạn chưa có bản ghi nội thất phòng nào."
            : $"Bạn có {furnitures.Count} bản ghi nội thất hoạt động. " +
              $"Theo trạng thái: {string.Join("; ", byStatus)}. " +
              $"Danh mục nổi bật: {string.Join("; ", byCatalog)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleRoomOccupancy(int landlordId, string message)
    {
        var occupancies = await _propertyRepo.GetRoomOccupanciesByLandlordAsync(landlordId);
        var active = occupancies.Count(o => o.Status == "active");
        var inactive = occupancies.Count(o => o.Status != "active");
        var primary = occupancies.Count(o => o.IsPrimary);
        var movedOut = occupancies.Count(o => o.MoveOutDate.HasValue);

        var info = occupancies.Count == 0
            ? "Bạn chưa có dữ liệu cư trú/phòng ở."
            : $"Bạn có {occupancies.Count} bản ghi cư trú: active {active}, inactive {inactive}, primary {primary}, đã chuyển ra {movedOut}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleFeeType(int landlordId, string message)
    {
        var feeTypes = await _feeTypeRepo.GetAllAsync();
        var systemTypes = feeTypes.Count(f => f.IsSystem);
        var customTypes = feeTypes.Count - systemTypes;
        var units = feeTypes.GroupBy(f => f.Unit).Select(g => $"{g.Key}: {g.Count()}");
        var info = feeTypes.Count == 0
            ? "Bạn chưa có loại phí nào."
            : $"Hiện có {feeTypes.Count} loại phí (hệ thống: {systemTypes}, tùy chỉnh: {customTypes}). " +
              $"Theo đơn vị: {string.Join("; ", units)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleFeeSetting(int landlordId, string message)
    {
        var settings = await _feeSettingRepo.GetFeeSettingsByLandlordAsync(landlordId);
        var active = settings.Count(s => s.EffectiveTo == null || s.EffectiveTo >= DateOnly.FromDateTime(DateTime.Today));
        var propertyLevel = settings.Count(s => s.PropertyId.HasValue && !s.RoomId.HasValue);
        var roomLevel = settings.Count(s => s.RoomId.HasValue);
        var topFeeTypes = settings
            .GroupBy(s => s.FeeType?.Name ?? "unknown")
            .Select(g => $"{g.Key}: {g.Count()}")
            .OrderByDescending(x => x)
            .Take(3);

        var info = settings.Count == 0
            ? "Bạn chưa có thiết lập phí nào."
            : $"Bạn có {settings.Count} thiết lập phí (đang hiệu lực: {active}, cấp khu trọ: {propertyLevel}, cấp phòng: {roomLevel}). " +
              $"Nhóm phí nhiều nhất: {string.Join("; ", topFeeTypes)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleInvoiceLine(int landlordId, string message)
    {
        var insight = await _invoiceLineRepo.GetInvoiceLineInsightByLandlordAsync(landlordId);

        var feeTypeBreakdown = insight.TopFeeTypes.Count == 0
            ? "không có nhóm phí nổi bật"
            : string.Join(
                "; ",
                insight.TopFeeTypes.Select(x => $"{x.FeeTypeName}: {x.LineCount} dòng, {x.TotalAmount:N0} VND"));

        var info = insight.TotalLines == 0
            ? "Bạn chưa có dòng chi tiết hóa đơn nào."
            : $"Bạn có {insight.TotalLines} dòng chi tiết hóa đơn, tổng giá trị {insight.TotalAmount:N0} VND. " +
              $"Top nhóm phí: {feeTypeBreakdown}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleStoredFile(int landlordId, string message)
    {
        var files = await _roomFurnitureRepo.GetStoredFilesByLandlordAsync(landlordId);
        var mimeTop = files.GroupBy(f => f.MimeType).Select(g => $"{g.Key}: {g.Count()}").Take(3);
        var recentFiles = files.Take(3).Select(f => f.FileName);

        var info = files.Count == 0
            ? "Bạn chưa có tệp lưu trữ nào."
            : $"Bạn có {files.Count} tệp lưu trữ. Định dạng phổ biến: {string.Join("; ", mimeTop)}. " +
              $"Tệp gần nhất: {string.Join(", ", recentFiles)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleStoredFileReference(int landlordId, string message)
    {
        var refs = await _roomFurnitureRepo.GetStoredFileReferencesByLandlordAsync(landlordId);
        var byType = refs.GroupBy(r => r.RefType).Select(g => $"{g.Key}: {g.Count()}");

        var info = refs.Count == 0
            ? "Bạn chưa có liên kết tệp nào."
            : $"Bạn có {refs.Count} liên kết tệp đến đối tượng nghiệp vụ. Theo loại: {string.Join("; ", byType)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleSubscription(int landlordId, string message)
    {
        var subscriptions = await _propertyRepo.GetSubscriptionsByLandlordAsync(landlordId);
        var activeSubscriptions = subscriptions.Count(s => s.Status == "active");
        var expired = subscriptions.Count(s => s.EndDate < DateOnly.FromDateTime(DateTime.Today));
        var byPlan = subscriptions.GroupBy(s => s.PlanName).Select(g => $"{g.Key}: {g.Count()}");

        var info = subscriptions.Count == 0
            ? "Bạn chưa có gói dịch vụ nào."
            : $"Bạn có {subscriptions.Count} gói dịch vụ, đang hoạt động: {activeSubscriptions}, đã hết hạn: {expired}. " +
              $"Theo gói: {string.Join("; ", byPlan)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleBankAccount(int landlordId, string message)
    {
        var accounts = await _propertyRepo.GetBankAccountsByLandlordAsync(landlordId);
        var primary = accounts.Count(a => a.IsPrimary);
        var byBank = accounts.GroupBy(a => a.BankName).Select(g => $"{g.Key}: {g.Count()}");

        var info = accounts.Count == 0
            ? "Bạn chưa có tài khoản ngân hàng nào."
            : $"Bạn có {accounts.Count} tài khoản ngân hàng, tài khoản chính: {primary}. " +
              $"Theo ngân hàng: {string.Join("; ", byBank)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleFurnitureCatalog(int landlordId, string message)
    {
        var catalogs = await _roomFurnitureRepo.GetFurnitureCatalogsAsync();
        var active = catalogs.Count(c => c.IsActive);
        var newest = catalogs.OrderByDescending(c => c.CreatedAt).Take(3).Select(c => c.Name);
        var info = catalogs.Count == 0
            ? "Chưa có danh mục nội thất."
            : $"Danh mục nội thất hiện có {catalogs.Count} mục (đang active: {active}). " +
              $"Mục mới: {string.Join(", ", newest)}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleFurnitureStatus(int landlordId, string message)
    {
        var statuses = await _roomFurnitureRepo.GetFurnitureStatusesAsync();
        var info = statuses.Count == 0
            ? "Chưa có trạng thái nội thất."
            : $"Danh mục trạng thái nội thất có {statuses.Count} trạng thái: {string.Join(", ", statuses.Select(s => s.Name))}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleLandlord(int landlordId, string message)
    {
        var landlord = await _propertyRepo.GetLandlordProfileAsync(landlordId);
        var properties = await _propertyRepo.GetPropertiesByLandlordIdAsync(landlordId);
        var tenantCount = (await _tenantRepo.GetTenantsByLandlordAsync(landlordId)).Count;
        var activeSubscriptions = await _propertyRepo.GetActiveSubscriptionCountByLandlordAsync(landlordId);

        var info = landlord == null
            ? "Không tìm thấy hồ sơ chủ trọ của bạn."
            : $"Hồ sơ chủ trọ: {landlord.DisplayName}, tạo từ {landlord.CreatedAt:dd/MM/yyyy}. " +
              $"Tài sản: {properties.Count}, người thuê: {tenantCount}, gói đang hoạt động: {activeSubscriptions}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }

    private async Task<string> HandleGuide(int landlordId, string message)
    {
        var lower = message.ToLower();

        // Guide ngắn gọn + link điều hướng trực tiếp theo route đang có thật.
        var guides = new Dictionary<string, string[]>
        {
            { "hợp đồng", new[] { "/Contract" } },
            { "phòng", new[] { "/Property" } },
            { "hóa đơn", new[] { "/Invoice", "/Invoice/History" } },
            { "thanh toán", new[] { "/PaymentSettings", "/Invoice" } },
            { "tài sản", new[] { "/Property", "/Property/Create" } },
            { "người thuê", new[] { "/Tenant/Create" } },
            { "thông báo", new[] { "/Notification/Send" } },
            { "báo cáo", new[] { "/Report/Revenue" } },
            { "thuế", new[] { "/Tax", "/Tax/Calculate" } },
            { "tài khoản", new[] { "/Account/Profile" } },
            { "dashboard", new[] { "/Dashboard" } }
        };

        var matched = guides
            .Where(g => lower.Contains(g.Key))
            .Select(g => $"- {g.Key}: {string.Join(" | ", g.Value)}")
            .ToList();

        if (matched.Count > 0)
        {
            return await Task.FromResult("Link dieu huong:\n" + string.Join("\n", matched));
        }

        var quick = new[]
        {
            "- hop dong: /Contract",
            "- phong/tai san: /Property",
            "- hoa don: /Invoice",
            "- thanh toan: /PaymentSettings",
            "- thong bao: /Notification/Send",
            "- bao cao: /Report/Revenue",
            "- thue: /Tax",
            "- tai khoan: /Account/Profile"
        };

        return await Task.FromResult("Guide nhanh (bam link de mo):\n" + string.Join("\n", quick));
    }
    

    public async Task<List<ChatMessage>> GetHistoryAsync(int userId)
    {
        // Lấy toàn bộ lịch sử chat của user (hoặc chỉ lấy 20-50 tin nhắn gần nhất nếu muốn)
        return await _repo.GetRecentMessagesAsync(userId);
    }
}


   