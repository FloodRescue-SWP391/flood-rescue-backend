
namespace FloodRescue.Services.Implements
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Upload file trả về URL
        /// </summary>
        /// <param name="file">File từ client</param>
        /// <param name="folder">Thư mục con (avatars, documents...)</param>
        /// <returns>URL của file đã upload</returns>
        Task<string> UploadFileAsync(Stream file, string fileName, string folder);

        /// <summary>
        /// Xóa file đã upload
        /// </summary>
        /// <param name="url">URL của file cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        Task<bool> DeleteFileAsync(string imageUrl);

        bool IsValidImage(string fileName,long fileSize);
    }
}