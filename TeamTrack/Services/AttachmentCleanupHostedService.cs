//using Microsoft.EntityFrameworkCore;
//using TeamTrack.Data;

//namespace TeamTrack.Services
//{
//    public class AttachmentCleanupHostedService : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<AttachmentCleanupHostedService> _logger;

//        public AttachmentCleanupHostedService(IServiceProvider serviceProvider, ILogger<AttachmentCleanupHostedService> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("Attachment Cleanup Background Service starting.");

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await CleanupOldAttachmentsAsync();
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error occurred executing attachment cleanup.");
//                }

//                // Run every 12 hours
//                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
//            }
//        }

//        private async Task CleanupOldAttachmentsAsync()
//        {
//            using var scope = _serviceProvider.CreateScope();
//            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

//            var thresholdDate = DateTime.UtcNow.AddDays(-7);

//            // 1. Process Completed WorkItems
//            var doneWorkItems = await dbContext.WorkItems
//                .Where(w => (w.Status == "completed" || w.Status == "closed") 
//                            && w.UpdatedAt <= thresholdDate 
//                            && !string.IsNullOrEmpty(w.AttachmentUrls))
//                .ToListAsync();

//            if (doneWorkItems.Count > 0)
//            {
//                _logger.LogInformation("Found {Count} done work items older than 7 days for attachment cleanup.", doneWorkItems.Count);
//                foreach (var item in doneWorkItems)
//                {
//                    var urls = item.AttachmentUrls!.Split(',', StringSplitOptions.RemoveEmptyEntries);
//                    foreach (var url in urls)
//                    {
//                        try
//                        {
//                            fileService.DeleteScreenshot(url.Trim());
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.LogWarning(ex, "Failed to delete file {Url} for work item {Id}", url, item.Id);
//                        }
//                    }
//                    item.AttachmentUrls = null;
//                    item.UpdatedAt = DateTime.UtcNow;
//                }
//                await dbContext.SaveChangesAsync();
//            }

//            // 2. Process Completed Bugs
//            var fixedBugs = await dbContext.Bugs
//                .Where(b => (b.Status == "fixed" || b.Status == "closed") 
//                            && b.UpdatedAt <= thresholdDate 
//                            && !string.IsNullOrEmpty(b.ScreenshotUrl))
//                .ToListAsync();

//            if (fixedBugs.Count > 0)
//            {
//                _logger.LogInformation("Found {Count} fixed bugs older than 7 days for screenshot cleanup.", fixedBugs.Count);
//                foreach (var bug in fixedBugs)
//                {
//                    try
//                    {
//                        fileService.DeleteScreenshot(bug.ScreenshotUrl!);
//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogWarning(ex, "Failed to delete screenshot {Url} for bug {Id}", bug.ScreenshotUrl, bug.Id);
//                    }
//                    bug.ScreenshotUrl = null;
//                    bug.UpdatedAt = DateTime.UtcNow;
//                }
//                await dbContext.SaveChangesAsync();
//            }
//        }
//    }
//}

using Microsoft.EntityFrameworkCore;
using TeamTrack.Data;

namespace TeamTrack.Services
{
    public class AttachmentCleanupHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttachmentCleanupHostedService> _logger;

        public AttachmentCleanupHostedService(IServiceProvider serviceProvider, ILogger<AttachmentCleanupHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Attachment Cleanup Background Service starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldAttachmentsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing attachment cleanup.");
                }

                // Run every 12 hours
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }

        // Only VIDEO attachments get auto-deleted. Images, PDFs, docs, etc. are never touched by this job.
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };

        private static bool IsVideoUrl(string url)
        {
            var ext = Path.GetExtension(url).ToLower();
            return VideoExtensions.Contains(ext);
        }

        private async Task CleanupOldAttachmentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

            // Videos get deleted 2 days after the item is marked resolved/closed
            var videoThresholdDate = DateTime.UtcNow.AddDays(-2);

            // 1. Process Completed/Closed WorkItems — delete ONLY video attachments, keep everything else
            var doneWorkItems = await dbContext.WorkItems
                .Where(w => (w.Status == "completed" || w.Status == "closed")
                            && w.UpdatedAt <= videoThresholdDate
                            && !string.IsNullOrEmpty(w.AttachmentUrls))
                .ToListAsync();

            if (doneWorkItems.Count > 0)
            {
                var changed = false;
                foreach (var item in doneWorkItems)
                {
                    var urls = item.AttachmentUrls!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(u => u.Trim())
                        .ToList();

                    var remainingUrls = new List<string>();
                    var deletedAny = false;

                    foreach (var url in urls)
                    {
                        if (!IsVideoUrl(url))
                        {
                            // Not a video — keep it forever, never touched by this job
                            remainingUrls.Add(url);
                            continue;
                        }

                        try
                        {
                            fileService.DeleteScreenshot(url);
                            deletedAny = true;
                            _logger.LogInformation("Deleted video attachment {Url} for work item {Id} (resolved/closed > 2 days ago).", url, item.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete video {Url} for work item {Id}", url, item.Id);
                            remainingUrls.Add(url); // keep it if deletion failed, retry next cycle
                        }
                    }

                    if (deletedAny)
                    {
                        item.AttachmentUrls = remainingUrls.Count > 0 ? string.Join(",", remainingUrls) : null;
                        changed = true;
                        // Note: UpdatedAt is intentionally NOT touched here, so any remaining
                        // non-video attachments keep their original resolved/closed timestamp reference.
                    }
                }

                if (changed)
                    await dbContext.SaveChangesAsync();
            }

            // 2. Process Fixed/Closed Bugs — delete the screenshot ONLY if it is a video
            var fixedBugs = await dbContext.Bugs
                .Where(b => (b.Status == "fixed" || b.Status == "closed")
                            && b.UpdatedAt <= videoThresholdDate
                            && !string.IsNullOrEmpty(b.ScreenshotUrl))
                .ToListAsync();

            if (fixedBugs.Count > 0)
            {
                var changed = false;
                foreach (var bug in fixedBugs)
                {
                    if (!IsVideoUrl(bug.ScreenshotUrl!))
                        continue; // image/pdf/etc — never delete

                    try
                    {
                        fileService.DeleteScreenshot(bug.ScreenshotUrl!);
                        bug.ScreenshotUrl = null;
                        changed = true;
                        _logger.LogInformation("Deleted video screenshot for bug {Id} (fixed/closed > 2 days ago).", bug.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete video screenshot for bug {Id}", bug.Id);
                    }
                }

                if (changed)
                    await dbContext.SaveChangesAsync();
            }
        }
    }
}
