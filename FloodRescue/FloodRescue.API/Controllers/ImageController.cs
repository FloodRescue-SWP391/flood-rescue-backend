using FloodRescue.Repositories.Interface;
using FloodRescue.Services.BusinessModels;
using FloodRescue.Services.DTO.Request.AuthRequest;
using FloodRescue.Services.Implements;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace FloodRescue.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ImageController> _logger;
        public ImageController(IFileStorageService fileStorageService, IUnitOfWork unitOfWork, ILogger<ImageController> logger)
        {
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpPost("upload/avatar/{userId}")]
        public async Task<IActionResult> UploadAvatar(Guid userId, IFormFile file)
        {
            try
            {
                _logger.LogInformation("Uploading avatar for user {UserId}", userId);



                // 1. Kiểm tra file (Không cần check Request.HasFormContentType nữa vì ASP.NET tự làm)
                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<object>.Fail("No file uploaded", 400));
                // 2. Kiểm tra user tồn tại
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return NotFound(ApiResponse<object>.Fail("User not found", 404));

                // 3. Validate file
                if (!_fileStorageService.IsValidImage(file.FileName, file.Length))
                {
                    return BadRequest(ApiResponse<object>.Fail(
                        "Invalid image file. Supported formats: JPG, JPEG, PNG, GIF, WEBP. Max size: 5MB",
                        400));
                }

                // 4. Upload file TRỰC TIẾP với Stream
                using var stream = file.OpenReadStream();
                var newImageUrl = await _fileStorageService.UploadFileAsync(
                    stream,
                    file.FileName,// Lưu ý: file.FileName là tên file gốc
                    "avatars");

                // 5. Xóa avatar cũ nếu có
                if (!string.IsNullOrEmpty(user.AvatarUrl)) 
                {
                    try
                    {
                        // Gọi hàm xóa file cũ, không cần xóa record trong DB vì ta chỉ update field
                        await _fileStorageService.DeleteFileAsync(user.AvatarUrl);
                    }
                    catch (Exception ex)
                    {
                        // Log warning nhưng không chặn quy trình, vì ảnh mới đã upload xong
                        _logger.LogWarning(ex, "Could not delete old avatar for user {UserId}", userId);
                    }
                    
                }
                // 6. Cập nhật thông tin vào database (Update User Entity)
                user.AvatarUrl = newImageUrl;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Avatar updated successfully for user {UserId}", userId);
                return Ok(ApiResponse<object>.Ok(new { imageUrl = newImageUrl }, "Upload successful", 200));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
                return StatusCode(500, ApiResponse<object>.Fail("Upload failed", 500));
            }
        }


    }
}
