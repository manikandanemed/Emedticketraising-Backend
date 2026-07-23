//namespace TeamTrack.Services
//{
//    public class FileService : IFileService
//    {
//        private readonly IWebHostEnvironment _env;

//        public FileService(IWebHostEnvironment env)
//        {
//            _env = env;
//        }

//        public async Task<string> UploadScreenshotAsync(IFormFile file)
//        {
//            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip" };
//            var extension = Path.GetExtension(file.FileName).ToLower();

//            if (!allowedExtensions.Contains(extension))
//                throw new Exception("Only image files (jpg, jpeg, png, gif, webp), PDF, Word, Excel, text, and zip files are allowed.");

//            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "screenshots");

//            if (!Directory.Exists(uploadsFolder))
//                Directory.CreateDirectory(uploadsFolder);

//            var fileName = $"{Guid.NewGuid()}{extension}";
//            var filePath = Path.Combine(uploadsFolder, fileName);

//            using (var stream = new FileStream(filePath, FileMode.Create))
//            {
//                await file.CopyToAsync(stream);
//            }

//            return $"/uploads/screenshots/{fileName}";
//        }

//        public void DeleteScreenshot(string fileUrl)
//        {
//            if (string.IsNullOrEmpty(fileUrl)) return;

//            var filePath = Path.Combine(_env.WebRootPath, fileUrl.TrimStart('/'));
//            if (File.Exists(filePath))
//                File.Delete(filePath);
//        }
//    }
//}


using Xabe.FFmpeg;

namespace TeamTrack.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private const long MaxVideoTargetSizeBytes = 50L * 1024 * 1024; // 50 MB
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadScreenshotAsync(IFormFile file)
        {
            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip",
                ".mp4", ".mov", ".avi", ".mkv", ".webm"
            };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                throw new Exception("Only image files (jpg, jpeg, png, gif, webp), PDF, Word, Excel, text, zip, and video files (mp4, mov, avi, mkv, webm) are allowed.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "screenshots");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var isVideo = VideoExtensions.Contains(extension);

            if (!isVideo)
            {
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/screenshots/{fileName}";
            }

            // ?? Video: save temporarily, compress if needed, then move to final location ??
            return await SaveVideoAsync(file, extension, uploadsFolder);
        }

        private async Task<string> SaveVideoAsync(IFormFile file, string extension, string uploadsFolder)
        {
            var tempFolder = Path.Combine(_env.WebRootPath, "uploads", "temp");
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            var tempInputPath = Path.Combine(tempFolder, $"{Guid.NewGuid()}{extension}");

            try
            {
                using (var stream = new FileStream(tempInputPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Already under target size — no need to re-encode, just move it as-is
                if (file.Length <= MaxVideoTargetSizeBytes)
                {
                    var directFileName = $"{Guid.NewGuid()}{extension}";
                    var directPath = Path.Combine(uploadsFolder, directFileName);
                    File.Move(tempInputPath, directPath);
                    return $"/uploads/screenshots/{directFileName}";
                }

                // Bigger than 50 MB — compress to fit under the target size
                var outputFileName = $"{Guid.NewGuid()}.mp4";
                var outputPath = Path.Combine(uploadsFolder, outputFileName);

                var mediaInfo = await FFmpeg.GetMediaInfo(tempInputPath);
                var durationSeconds = mediaInfo.Duration.TotalSeconds;

                if (durationSeconds <= 0)
                    throw new Exception("Could not read video duration — the file may be corrupted.");

                const long audioBitrate = 128_000; // 128 kbps
                var targetSizeBits = MaxVideoTargetSizeBytes * 8;
                var videoBitrate = (long)(targetSizeBits / durationSeconds) - audioBitrate;
                videoBitrate = (long)(videoBitrate * 0.92); // safety margin for container overhead

                if (videoBitrate < 150_000)
                    videoBitrate = 150_000; // floor so extremely long videos don't end up unwatchable

                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

                if (videoStream == null)
                    throw new Exception("No video stream found in the uploaded file.");

                videoStream.SetCodec(VideoCodec.libx264);
                videoStream.SetBitrate(videoBitrate);

                var conversion = FFmpeg.Conversions.New()
                    .AddStream(videoStream);

                if (audioStream != null)
                {
                    audioStream.SetCodec(AudioCodec.aac);
                    audioStream.SetBitrate(audioBitrate);
                    conversion.AddStream(audioStream);
                }

                conversion.SetOutput(outputPath);
                conversion.SetOverwriteOutput(true);

                await conversion.Start();

                return $"/uploads/screenshots/{outputFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Video processing failed: {ex.Message}");
            }
            finally
            {
                if (File.Exists(tempInputPath))
                    File.Delete(tempInputPath);
            }
        }

        public void DeleteScreenshot(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            var filePath = Path.Combine(_env.WebRootPath, fileUrl.TrimStart('/'));
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
