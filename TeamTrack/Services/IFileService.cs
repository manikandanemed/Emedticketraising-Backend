namespace TeamTrack.Services
{
    public interface IFileService
    {
        Task<string> UploadScreenshotAsync(IFormFile file);
        void DeleteScreenshot(string fileUrl);
    }
}
