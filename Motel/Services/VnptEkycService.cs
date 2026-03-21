using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Motel.Models;
using Motel.Services.Interfaces;

namespace Motel.Services;

public class VnptEkycService : IVnptEkycService
{
    private readonly HttpClient _httpClient;
    private readonly VnptEkycOptions _options;
    private readonly ILogger<VnptEkycService> _logger;

    public VnptEkycService(HttpClient httpClient, IOptions<VnptEkycOptions> options, ILogger<VnptEkycService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OcrResult?> ScanIdCardAsync(IFormFile file)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.TokenId) || string.IsNullOrEmpty(_options.TokenKey) || _options.TokenId == "YOUR_TOKEN_ID_HERE")
            {
                // Return mock data for testing if no real token is provided
                _logger.LogWarning("VNPT eKYC tokens not configured. Returning mock output.");
                await Task.Delay(1500); // Simulate network latency
                return new OcrResult
                {
                    Id = "079090012345",
                    Name = "NGUYỄN VĂN A",
                    Birthday = "01/01/1990",
                    Address = "Phường 1, Quận 1, TP Hồ Chí Minh"
                };
            }

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "file", file.FileName);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/ocr/id")
            {
                Content = content
            };
            request.Headers.Add("Token-id", _options.TokenId);
            request.Headers.Add("Token-key", _options.TokenKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("VNPT eKYC API returned {StatusCode}: {Reason}", response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var jsonDocument = JsonDocument.Parse(jsonString);
            
            var root = jsonDocument.RootElement;
            if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind != JsonValueKind.Null)
            {
                // In a real VNPT eKYC response, usually there's a list for data if it's multiple sides,
                // but for front side it's often a single object under "object" or directly.
                // Assuming it's directly mapped under data. The exact schema depends on VNPT doc.
                // Here we safely map some common attributes.
                if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
                {
                    dataElement = dataElement[0];
                }

                return new OcrResult
                {
                    Id = dataElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null,
                    Name = dataElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                    Birthday = dataElement.TryGetProperty("birthday", out var dobProp) ? dobProp.GetString() : null,
                    Address = dataElement.TryGetProperty("address", out var addrProp) || dataElement.TryGetProperty("recent_location", out addrProp) ? addrProp.GetString() : null,
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling VNPT eKYC");
            return null;
        }
    }
}
