using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motel.Data;
using Motel.Helpers;
using Motel.Models;
using Motel.Services.Interface;
using Motel.Services.Interfaces;
using System.Security.Claims;

namespace Motel.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EkycController : ControllerBase
{
    private readonly IEkycService _ekycService;
    private readonly LandlordHelper _landlordHelper;
    private readonly IFileService _fileService;

    public EkycController(
        IEkycService ekycService,
        LandlordHelper landlordHelper,
        IFileService fileService)
    {
        _ekycService = ekycService;
        _landlordHelper = landlordHelper;
        _fileService = fileService;
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

        var storedFile = await _fileService.UploadAndSaveFileAsync(file, "cccd", landlordId, userId);

        if (storedFile == null)
            return StatusCode(500, new { message = "Failed to upload image" });

        // Call OCR
        var result = await _ekycService.ScanIdCardAsync(file);

        if (result == null)
        {
            return Ok(new { 
                success = false, 
                message = "Không thể nhận dạng thẻ. Vui lòng thử lại với mặt trước ảnh rõ nét hơn.",
                storedFileId = storedFile.StoredFileId,
                storagePath = storedFile.StoragePath
            });
        }

        return Ok(new
        {
            success = true,
            storedFileId = storedFile.StoredFileId,
            storagePath = storedFile.StoragePath,
            data = result
        });
    }

    [HttpPost("scan-both")]
    public async Task<IActionResult> ScanBoth(IFormFile frontFile, IFormFile backFile)
    {
        if (frontFile == null || frontFile.Length == 0 || backFile == null || backFile.Length == 0)
            return BadRequest(new { message = "Thiếu mặt trước hoặc mặt sau CCCD" });

        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (landlordId <= 0 || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var frontFileRecord = await _fileService.UploadAndSaveFileAsync(frontFile, "cccd_front", landlordId, userId);
        var backFileRecord = await _fileService.UploadAndSaveFileAsync(backFile, "cccd_back", landlordId, userId);
        
        if (frontFileRecord == null || backFileRecord == null)
            return StatusCode(500, new { message = "Failed to upload images" });

        var frontId = frontFileRecord.StoredFileId;
        var frontPath = frontFileRecord.StoragePath;
        var backId = backFileRecord.StoredFileId;
        var backPath = backFileRecord.StoragePath;

        var result = await _ekycService.ScanBothIdCardsAsync(frontFile, backFile);

        if (result == null)
        {
            return Ok(new
            {
                success = false,
                message = "Không thể nhận dạng thẻ. Vui lòng kiểm tra lại ảnh chụp mặt trước/sau.",
                frontId, backId, frontPath, backPath
            });
        }

        return Ok(new
        {
            success = true,
            frontId, backId, frontPath, backPath,
            data = result
        });
    }
}

