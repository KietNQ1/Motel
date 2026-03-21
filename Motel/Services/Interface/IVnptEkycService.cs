using Microsoft.AspNetCore.Http;

namespace Motel.Services.Interfaces;

public interface IVnptEkycService
{
    Task<OcrResult?> ScanIdCardAsync(IFormFile file);
}

public class OcrResult
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Birthday { get; set; }
    public string? Address { get; set; }
}
