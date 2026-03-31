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
        ITenantRepository tenantRepo)
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
        };
        var lower = message.ToLower();
        foreach (var kv in map)
            if (kv.Value.Any(k => lower.Contains(k))) return kv.Key;
        return "other";
    }

    // Xử lý từng nghiệp vụ (có thể mở rộng, tách file riêng nếu lớn)
    private async Task<string> HandleContract(int userId, string message)
    {
        var contracts = await _contractRepo.GetTenantsByLandlordAsync(userId, default);
        var info = contracts.Count == 0
            ? "Bạn chưa có hợp đồng nào trong hệ thống."
            : $"Bạn có {contracts.Count} hợp đồng với các tenant: {string.Join(", ", contracts.Select(t => t.FullName))}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleRoom(int userId, string message)
    {
        var rooms = await _roomRepo.GetRoomsByLandlordAsync(userId);
        var info = rooms.Count == 0
            ? "Bạn chưa có phòng nào trong hệ thống."
            : $"Bạn có {rooms.Count} phòng: {string.Join(", ", rooms.Select(r => r.RoomName))}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleInvoice(int userId, string message)
    {
        var invoices = await _invoiceRepo.GetInvoicesByLandlordAsync(userId);
        var info = invoices.Count == 0
            ? "Bạn chưa có hóa đơn nào trong hệ thống."
            : $"Bạn có {invoices.Count} hóa đơn, tổng tiền: {invoices.Sum(i => i.TotalAmount):N0} VND.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleTransaction(int userId, string message)
    {
        var trans = await _transactionRepo.GetTransactionsByUserIdAsync(userId);
        var info = trans.Count == 0
            ? "Bạn chưa có giao dịch nào."
            : $"Bạn có {trans.Count} giao dịch, tổng tiền: {trans.Sum(t => t.Amount):N0} VND.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleNotification(int userId, string message)
    {
        var noti = await _notificationRepo.GetNotificationsByUserIdAsync(userId);
        var info = noti.Count == 0
            ? "Bạn chưa có thông báo nào."
            : $"Bạn có {noti.Count} thông báo gần đây.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleTax(int userId, string message)
    {
        var taxes = await _taxRepo.GetTaxEstimationsByUserIdAsync(userId);
        var info = taxes.Count == 0
            ? "Bạn chưa có dữ liệu thuế nào."
            : $"Bạn có {taxes.Count} bản ước tính thuế.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleMeter(int userId, string message)
    {
        var meters = await _meterRepo.GetMeterReadingsByUserIdAsync(userId);
        var info = meters.Count == 0
            ? "Bạn chưa có chỉ số điện nước nào."
            : $"Bạn có {meters.Count} bản ghi chỉ số điện nước.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    private async Task<string> HandleTenant(int userId, string message)
    {
        var tenants = await _tenantRepo.GetTenantsByLandlordAsync(userId);
        var info = tenants.Count == 0
            ? "Bạn chưa có người thuê nào."
            : $"Bạn có {tenants.Count} người thuê: {string.Join(", ", tenants.Select(t => t.FullName))}.";
        return await _gemini.GenerateReplyAsync(new List<ChatMessage> {
            new ChatMessage { Role = "system", Content = info }
        }, message);
    }
    

    public async Task<List<ChatMessage>> GetHistoryAsync(int userId)
    {
        // Lấy toàn bộ lịch sử chat của user (hoặc chỉ lấy 20-50 tin nhắn gần nhất nếu muốn)
        return await _repo.GetRecentMessagesAsync(userId);
    }
}


   