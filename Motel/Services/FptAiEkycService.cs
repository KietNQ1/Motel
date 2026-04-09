using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Motel.Models;
using Motel.Services.Interfaces;
using System.Net.Http.Headers;

namespace Motel.Services;

public class FptAiEkycService : IEkycService
{
    private readonly HttpClient _httpClient;
    private readonly FptAiEkycOptions _options;
    private readonly ILogger<FptAiEkycService> _logger;

    public FptAiEkycService(HttpClient httpClient, IOptions<FptAiEkycOptions> options, ILogger<FptAiEkycService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OcrResult?> ScanIdCardAsync(IFormFile file)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.ApiKey) || _options.ApiKey == "YOUR_FPT_API_KEY_HERE")
            {
                _logger.LogWarning("FPT AI API Key not configured. Returning mock output.");
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

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "image/jpeg");
            content.Add(streamContent, "image", file.FileName);

            var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl)
            {
                Content = content
            };
            request.Headers.Add("api-key", _options.ApiKey);

            var response = await _httpClient.SendAsync(request);
            var jsonString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("FPT AI Raw Response: {Json}", jsonString);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("FPT AI API returned {StatusCode}: {Reason} - {Content}", response.StatusCode, response.ReasonPhrase, jsonString);
                return null;
            }

            using var jsonDocument = JsonDocument.Parse(jsonString);
            var root = jsonDocument.RootElement;
                
            if (root.TryGetProperty("errorCode", out var errorCode) && errorCode.GetInt32() == 0)
            {
                if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                {
                    var data = dataArray[0];
                    // Extract fields
                    string GetStringValue(string propName)
                    {
                        if (data.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
                        {
                            var val = prop.GetString();
                            return val == "N/A" ? null : val;
                        }
                        return null;
                    }

                    var id = GetStringValue("id");
                    var name = GetStringValue("name");
                    var dob = GetStringValue("dob");
                    var address = GetStringValue("address") 
                                  ?? GetStringValue("home") 
                                  ?? GetStringValue("place_of_residence")
                                  ?? GetStringValue("resident_address")
                                  ?? GetStringValue("resident");
                    var sex = GetStringValue("sex");
                        
                    return new OcrResult
                    {
                        Id = id,
                        Name = name,
                        Birthday = dob,
                        Address = address,
                        Gender = sex
                    };
                }
            }
            else
            {
                var errorMsg = root.TryGetProperty("errorMessage", out var errMsg) ? errMsg.GetString() : "Unknown error";
                _logger.LogError("FPT AI returned error code {ErrorCode}: {ErrorMessage}", errorCode.GetInt32(), errorMsg);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling FPT AI");
            return null;
        }
    }

    public async Task<OcrResult?> ScanBothIdCardsAsync(IFormFile frontFile, IFormFile backFile)
    {
        var frontResultTask = ScanIdCardAsync(frontFile);
        var backResultTask = ScanIdCardAsync(backFile);

        await Task.WhenAll(frontResultTask, backResultTask);

        var frontResult = await frontResultTask;
        var backResult = await backResultTask;

        if (frontResult == null && backResult == null) return null;

        var finalResult = frontResult ?? new OcrResult();

        if (backResult != null)
        {
            finalResult.Id ??= backResult.Id;
            finalResult.Name ??= backResult.Name;
            finalResult.Birthday ??= backResult.Birthday;
            finalResult.Gender ??= backResult.Gender;
            finalResult.Address ??= backResult.Address;
        }

        return finalResult;
    }
}
