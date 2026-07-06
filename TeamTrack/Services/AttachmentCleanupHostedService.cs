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

        private async Task CleanupOldAttachmentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

            var thresholdDate = DateTime.UtcNow.AddDays(-7);

            // 1. Process Completed WorkItems
            var doneWorkItems = await dbContext.WorkItems
                .Where(w => (w.Status == "completed" || w.Status == "closed") 
                            && w.UpdatedAt <= thresholdDate 
                            && !string.IsNullOrEmpty(w.AttachmentUrls))
                .ToListAsync();

            if (doneWorkItems.Count > 0)
            {
                _logger.LogInformation("Found {Count} done work items older than 7 days for attachment cleanup.", doneWorkItems.Count);
                foreach (var item in doneWorkItems)
                {
                    var urls = item.AttachmentUrls!.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var url in urls)
                    {
                        try
                        {
                            fileService.DeleteScreenshot(url.Trim());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete file {Url} for work item {Id}", url, item.Id);
                        }
                    }
                    item.AttachmentUrls = null;
                    item.UpdatedAt = DateTime.UtcNow;
                }
                await dbContext.SaveChangesAsync();
            }

            // 2. Process Completed Bugs
            var fixedBugs = await dbContext.Bugs
                .Where(b => (b.Status == "fixed" || b.Status == "closed") 
                            && b.UpdatedAt <= thresholdDate 
                            && !string.IsNullOrEmpty(b.ScreenshotUrl))
                .ToListAsync();

            if (fixedBugs.Count > 0)
            {
                _logger.LogInformation("Found {Count} fixed bugs older than 7 days for screenshot cleanup.", fixedBugs.Count);
                foreach (var bug in fixedBugs)
                {
                    try
                    {
                        fileService.DeleteScreenshot(bug.ScreenshotUrl!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete screenshot {Url} for bug {Id}", bug.ScreenshotUrl, bug.Id);
                    }
                    bug.ScreenshotUrl = null;
                    bug.UpdatedAt = DateTime.UtcNow;
                }
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
