using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Motel.Models;
using Motel.Services.Interfaces;
using System.Net.Http.Headers;

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

    private async Task<string?> UploadFileAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        using var fileStream = file.OpenReadStream();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "image/jpeg");

        content.Add(streamContent, "file", file.FileName);
        content.Add(new StringContent(file.FileName), "title");
        content.Add(new StringContent("Uploaded CCCD Image"), "description");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/file-service/v1/addFile")
        {
            Content = content
        };
        request.Headers.Add("Token-id", _options.TokenId);
        request.Headers.Add("Token-key", _options.TokenKey);
        request.Headers.Add("mac-address", "TEST1");
        
        if (!string.IsNullOrEmpty(_options.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim());
        }

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("VNPT eKYC Upload API returned {StatusCode}: {Reason} - {Content}", response.StatusCode, response.ReasonPhrase, responseContent);
            return null;
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(jsonString);
        var root = jsonDocument.RootElement;
        
        if (root.TryGetProperty("object", out var objectElement) && 
            objectElement.TryGetProperty("hash", out var hashElement))
        {
            return hashElement.GetString();
        }
        
        return null;
    }

    public async Task<OcrResult?> ScanIdCardAsync(IFormFile file)
    {
        // Currently fallback method if someone only uploads front.
        // We will adapt if needed, but the main one is dual.
        return await ScanBothIdCardsAsync(file, file); // fallback
    }

    public async Task<OcrResult?> ScanBothIdCardsAsync(IFormFile frontFile, IFormFile backFile)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.TokenId) || string.IsNullOrEmpty(_options.TokenKey) || _options.TokenId == "YOUR_TOKEN_ID_HERE")
            {
                _logger.LogWarning("VNPT eKYC tokens not configured. Returning mock output.");
                await Task.Delay(1500); 
                return new OcrResult
                {
                    Id = "079090012345",
                    Name = "NGUYỄN VĂN A",
                    Birthday = "01/01/1990",
                    Address = "Phường 1, Quận 1, TP Hồ Chí Minh",
                    Gender = "Nam"
                };
            }

            var hashFront = await UploadFileAsync(frontFile);
            var hashBack = await UploadFileAsync(backFile);

            if (string.IsNullOrEmpty(hashFront) || string.IsNullOrEmpty(hashBack))
            {
                _logger.LogError("Failed to upload CCCD images to VNPT.");
                return null;
            }

            var payload = new
            {
                img_front = hashFront,
                img_back = hashBack,
                client_session = $"WEB_Motel_1.0_Simulator_1.0.0_{Guid.NewGuid()}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                type = 1,
                validate_postcode = true,
                token = Guid.NewGuid().ToString()
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/ai/v1/ocr/id")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Token-id", _options.TokenId);
            request.Headers.Add("Token-key", _options.TokenKey);
            request.Headers.Add("mac-address", "TEST1");
            
            if (!string.IsNullOrEmpty(_options.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim());
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("VNPT eKYC OCR API returned {StatusCode}: {Reason} - {Content}", response.StatusCode, response.ReasonPhrase, responseContent);
                return null;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var jsonDocument = JsonDocument.Parse(jsonString);
            
            var root = jsonDocument.RootElement;
            if (root.TryGetProperty("object", out var dataElement) && dataElement.ValueKind != JsonValueKind.Null)
            {
                return new OcrResult
                {
                    Id = dataElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null,
                    Name = dataElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                    Birthday = dataElement.TryGetProperty("birth_day", out var dobProp) ? dobProp.GetString() : null,
                    Address = dataElement.TryGetProperty("recent_location", out var addrProp) ? addrProp.GetString() : null,
                    Gender = dataElement.TryGetProperty("gender", out var sexProp) ? sexProp.GetString() : null,
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
