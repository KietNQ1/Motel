using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Motel.Models;
using Motel.Services.Interface;
using System.Text.Json.Serialization;
using System.Net.Http.Json; // Cần thiết cho PostAsJsonAsync

namespace Motel.Services;

// --- DTOs cho Mapping JSON (Giữ nguyên) ---
public class GeminiResponse {
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
}
public class Candidate {
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}
public class GeminiContent {
    [JsonPropertyName("parts")]
    public List<GeminiPart>? Parts { get; set; }
}
public class GeminiPart {
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

// --- Class chính ---
public class GeminiService : IGeminiService {
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiService(HttpClient httpClient, IConfiguration configuration) {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? ""; 
    }

    public async Task<string> GenerateReplyAsync(List<ChatMessage> history, string userMessage) 
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

        var payload = new {
            contents = history.Select(h => new {
                role = h.Role == "user" ? "user" : "model",
                parts = new[] { new { text = h.Content } }
            }).Append(new { 
                role = "user", 
                parts = new[] { new { text = userMessage } } 
            }).ToList(),
            generationConfig = new {
                temperature = 1,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 8192,
                responseMimeType = "text/plain"
            }
        };

        try 
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload);
            
            if (!response.IsSuccessStatusCode) {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Lỗi Google API: {errorBody}");
                return "Trợ lý ảo đang bận, thử lại sau nhé.";
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            
            return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text 
                   ?? "Tôi không tìm thấy câu trả lời phù hợp.";
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"LỖI KẾT NỐI: {ex.Message}");
            return "Kết nối tới máy chủ AI thất bại.";
        }
    }
}
