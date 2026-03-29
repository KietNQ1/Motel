using Microsoft.AspNetCore.Http;

namespace Motel.Services.Interfaces;

public interface IEkycService
{
    Task<OcrResult?> ScanIdCardAsync(IFormFile file);
    Task<OcrResult?> ScanBothIdCardsAsync(IFormFile frontFile, IFormFile backFile);
}

public class OcrResult
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Birthday { get; set; }
    public string? Address { get; set; }
    public string? Gender { get; set; }
}
