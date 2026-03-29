using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Motel.Services.Interface;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
    Task<bool> DeleteImageAsync(string publicId);
}
