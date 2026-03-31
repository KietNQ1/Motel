using Microsoft.AspNetCore.Http;
using Motel.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Motel.Services.Interface;

public interface IFileService
{
    Task<StoredFile?> UploadAndSaveFileAsync(IFormFile file, string folder, int landlordId, int uploadedByUserId);
    Task<bool> DeleteFilesAsync(List<int> storedFileIds);
    Task<StoredFile?> ReplaceFileAsync(int oldStoredFileId, IFormFile newFile, string folder, int landlordId, int uploadedByUserId);
}
