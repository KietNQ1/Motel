using Microsoft.AspNetCore.Http;
using Motel.Data;
using Motel.Models;
using Motel.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Motel.Services;

public class FileService : IFileService
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly MotelDbContext _context;

    public FileService(ICloudinaryService cloudinaryService, MotelDbContext context)
    {
        _cloudinaryService = cloudinaryService;
        _context = context;
    }

    public async Task<StoredFile?> UploadAndSaveFileAsync(IFormFile file, string folder, int landlordId, int uploadedByUserId)
    {
        if (file == null || file.Length == 0)
            return null;

        // Upload to Cloudinary (or any downstream storage)
        var url = await _cloudinaryService.UploadImageAsync(file, folder);

        if (string.IsNullOrEmpty(url))
            return null;

        // Create the StoredFile record
        var storedFile = new StoredFile
        {
            FileName = file.FileName,
            MimeType = file.ContentType,
            StoragePath = url,
            UploadedAt = DateTime.Now,
            LandlordId = landlordId,
            UploadedByUserId = uploadedByUserId
        };

        _context.StoredFiles.Add(storedFile);
        await _context.SaveChangesAsync();

        return storedFile;
    }

    public async Task<bool> DeleteFilesAsync(List<int> storedFileIds)
    {
        if (storedFileIds == null || !storedFileIds.Any())
            return false;

        var files = await _context.StoredFiles
            .Where(f => storedFileIds.Contains(f.StoredFileId))
            .ToListAsync();

        foreach (var file in files)
        {
            if (!string.IsNullOrEmpty(file.StoragePath))
            {
                var publicId = ExtractPublicId(file.StoragePath);
                if (!string.IsNullOrEmpty(publicId))
                {
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }
            }

            _context.StoredFiles.Remove(file);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<StoredFile?> ReplaceFileAsync(int oldStoredFileId, IFormFile newFile, string folder, int landlordId, int uploadedByUserId)
    {
        // 1. Upload file mới -> Tạo StoredFile mới
        var newStoredFile = await UploadAndSaveFileAsync(newFile, folder, landlordId, uploadedByUserId);
        if (newStoredFile == null) return null;

        // 2. Lấy tham chiếu cũ
        var references = await _context.StoredFileReferences
            .Where(r => r.StoredFileId == oldStoredFileId)
            .ToListAsync();

        if (references.Any())
        {
            // Update StoredFileReference trỏ sang file mới
            foreach (var rf in references)
            {
                rf.StoredFileId = newStoredFile.StoredFileId;
            }
            await _context.SaveChangesAsync();
        }

        // 3. Xóa file cũ trên Cloud -> Xóa StoredFile cũ
        await DeleteFilesAsync(new List<int> { oldStoredFileId });

        return newStoredFile;
    }

    private string? ExtractPublicId(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            var uploadIndex = url.IndexOf("upload/");
            if (uploadIndex == -1) return null;

            var afterUpload = url.Substring(uploadIndex + 7);

            var parts = afterUpload.Split('/');
            if (parts.Length > 0 && parts[0].StartsWith("v") && parts[0].Length > 1 && char.IsDigit(parts[0][1]))
            {
                // Remove the version part "v12345"
                afterUpload = string.Join('/', parts.Skip(1));
            }

            // Remove the extension
            var lastDot = afterUpload.LastIndexOf('.');
            if (lastDot > 0)
            {
                afterUpload = afterUpload.Substring(0, lastDot);
            }

            return afterUpload;
        }
        catch
        {
            return null;
        }
    }
}
