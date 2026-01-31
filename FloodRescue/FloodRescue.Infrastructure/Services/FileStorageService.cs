using FloodRescue.Services.Implements;
using Microsoft.AspNetCore.Hosting; // Đã dùng được nhờ bước 2
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;

namespace FloodRescue.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<FileStorageService> _logger;


        // configuration
        private readonly string[] _allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5 MB


        public FileStorageService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor, ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        public async Task<bool> DeleteFileAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogWarning("DeleteFileAsync called with empty URL");
                    return false;
                }
                // 1. Chuyển URL thành đường dẫn tương đối
                var uri = new Uri(imageUrl);
                var relativePath = uri.AbsolutePath.TrimStart('/');

                // 2. Kiểm tra bảo mật : đảm bảo đường dẫn không vượt ra ngoài thư mục wwwroot
                if (!relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Attempted to delete file outside uploads directory: {Path}", relativePath);
                    return false;
                }
                // 3. Tạo đường dẫn tuyệt đối trên server
                var filePath = Path.Combine(_environment.WebRootPath, relativePath);
                // 4. Xóa file nếu tồn tại
                if(File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("File deleted: {FilePath}", filePath);
                    return true;
                }
                else
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Url}", imageUrl);
                return false;
            }
        }

        public bool IsValidImage(string fileName, long fileSize)
        {
            if (fileSize > _maxFileSize)
            {
                _logger.LogWarning("File size {FileSize} exceeds the maximum allowed size of {MaxFileSize}", fileSize, _maxFileSize);
                return false;
            }
            // Path.GetExtension(fileName) Dùng để lấy ra phần .png, .jpg, ...
            // .ToLowerInvariant() Chuyển về chữ thường
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension) || string.IsNullOrEmpty(extension))
            {
                _logger.LogWarning("File extension {Extension} is not allowed", extension);
                return false;
            }
            return true;
        }

        public async Task<string> UploadFileAsync(Stream file, string fileName, string folder)
        {
            try
            {
                // 1. validate file
                if (!IsValidImage(fileName, file.Length))
                {
                    throw new ArgumentException($"Invalid file. Allowed: {string.Join(", ", _allowedExtensions)}. Max: {_maxFileSize / 1024 / 1024}MB");
                }
                // 2. Tạo ra filename mới để tránh trùng lặp
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                // 3. Tạo đường dẫn tải lên
                // _environment.WebRootPath => FloodRescue.API\wwwroot
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", folder);

                // 4. Tạo thư mục nếu chưa tồn tại
                // thư mục avatars chưa tồn tại → tự động tạo.
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                    _logger.LogInformation("Created directory: {Path}", uploadPath);
                }

                // 5. Tạo đường dẫn file đầy đủ
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                // 6. Lưu file vào server
                //  Tạo file mới tại đường dẫn
                using (var outputStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(outputStream);
                }
                _logger.LogInformation("File uploaded: {FilePath}", filePath);

                // 7. Tạo URL trả về
                var request = _httpContextAccessor.HttpContext?.Request;
                if (request == null)
                {
                    throw new InvalidOperationException("HttpContext is not available");
                }

                var baseUrl = $"{request.Scheme}://{request.Host}";
                var fileUrl = $"{baseUrl}/uploads/{folder}/{uniqueFileName}";
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
                throw;
            }

        }
    }
}
