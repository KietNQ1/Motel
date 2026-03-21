using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motel.Data;
using Motel.Helpers;
using Motel.Models;
using Motel.Services.Interfaces;
using System.Security.Claims;

namespace Motel.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class VnptEkycController : ControllerBase
{
    private readonly IVnptEkycService _ekycService;
    private readonly MotelDbContext _context;
    private readonly LandlordHelper _landlordHelper;
    private readonly IWebHostEnvironment _env;

    public VnptEkycController(
        IVnptEkycService ekycService,
        MotelDbContext context,
        LandlordHelper landlordHelper,
        IWebHostEnvironment env)
    {
        _ekycService = ekycService;
        _context = context;
        _landlordHelper = landlordHelper;
        _env = env;
    }

    [HttpPost("scan")]
    public async Task<IActionResult> ScanCccd(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (landlordId <= 0 || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        // Save file to wwwroot/uploads/cccd
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "cccd");
        if (!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var storagePath = $"/uploads/cccd/{fileName}";

        var storedFile = new StoredFile
        {
            FileName = file.FileName,
            MimeType = file.ContentType,
            StoragePath = storagePath,
            UploadedAt = DateTime.Now,
            LandlordId = landlordId,
            UploadedByUserId = userId
        };

        _context.StoredFiles.Add(storedFile);
        await _context.SaveChangesAsync();

        // Call OCR
        var result = await _ekycService.ScanIdCardAsync(file);

        if (result == null)
        {
            return Ok(new { 
                success = false, 
                message = "Không thể nhận dạng thẻ. Vui lòng thử lại với ảnh rõ nét hơn.",
                storedFileId = storedFile.StoredFileId,
                storagePath = storagePath
            });
        }

        return Ok(new
        {
            success = true,
            storedFileId = storedFile.StoredFileId,
            storagePath = storagePath,
            data = result
        });
    }
}
