using Microsoft.EntityFrameworkCore;
using TeamTrack.DTOs;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Services
{
    public class BugService : IBugService
    {
        private readonly IRepository<Bug> _bugRepo;
        private readonly IFileService _fileService;
        private readonly IRepository<WorkItem> _workItemRepo;

        public BugService(IRepository<Bug> bugRepo, IFileService fileService, IRepository<WorkItem> workItemRepo)
        {
            _bugRepo = bugRepo;
            _fileService = fileService;
            _workItemRepo = workItemRepo;
        }

        public async Task<BugResponseDto> CreateBugAsync(CreateBugRequestDto request, int userId)
        {
            var count = await _bugRepo.Query().CountAsync();
            var bug = new Bug
            {
                BugNumber = $"BUG-{(count + 1):D3}",
                Title = request.Title,
                Description = request.Description,
                ScreenshotUrl = request.ScreenshotUrl,
                Status = "open",
                WorkItemId = request.WorkItemId,
                RaisedByUserId = userId,
                AssignedToUserId = request.AssignedToUserId,
                RaisedBuild = request.RaisedBuild,
                Severity = request.Severity,
                IssueType = request.IssueType ?? "New",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _bugRepo.AddAsync(bug);
            await _bugRepo.SaveAsync();

            var created = await _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .FirstOrDefaultAsync(b => b.Id == bug.Id);

            return MapToDto(created!);
        }

        public async Task<BugResponseDto> CreateBugWithScreenshotAsync(CreateBugRequestDto request, IFormFile? screenshot, int userId)
        {
            string? screenshotUrl = null;

            if (screenshot != null)
                screenshotUrl = await _fileService.UploadScreenshotAsync(screenshot);

            var count = await _bugRepo.Query().CountAsync();
            var bug = new Bug
            {
                BugNumber = $"BUG-{(count + 1):D3}",
                Title = request.Title,
                Description = request.Description,
                ScreenshotUrl = screenshotUrl,
                Status = "open",
                WorkItemId = request.WorkItemId,
                RaisedByUserId = userId,
                AssignedToUserId = request.AssignedToUserId,
                RaisedBuild = request.RaisedBuild,
                Severity = request.Severity,
                IssueType = request.IssueType ?? "New",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _bugRepo.AddAsync(bug);
            await _bugRepo.SaveAsync();

            var created = await _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .FirstOrDefaultAsync(b => b.Id == bug.Id);

            return MapToDto(created!);
        }

        public async Task<List<BugResponseDto>> GetBugsByWorkItemAsync(int workItemId)
        {
            var bugs = await _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .Where(b => b.WorkItemId == workItemId)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();

            return bugs.Select(MapToDto).ToList();
        }

        public async Task<List<BugResponseDto>> GetBugsByAssignedEmployeeAsync(int userId)
        {
            var bugs = await _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .Where(b => b.AssignedToUserId == userId)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();

            return bugs.Select(MapToDto).ToList();
        }

        public async Task<PagedResult<BugResponseDto>> GetBugsByAssignedEmployeePagedAsync(int userId, int page, int pageSize, string? status, string? date, string? search)
        {
            var query = _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .Where(b => b.AssignedToUserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
                query = query.Where(b => b.Status == status);

            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsedDate))
            {
                var startDate = parsedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var endDate = parsedDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                query = query.Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate);
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || b.BugNumber.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BugResponseDto>
            {
                Items      = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }

        public async Task<List<BugResponseDto>> GetAllBugsAsync()
        {
            var bugs = await _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();

            return bugs.Select(MapToDto).ToList();
        }

        public async Task<PagedResult<BugResponseDto>> GetAllBugsPagedAsync(int page, int pageSize, string? status, string? date, string? search)
        {
            var query = _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
                query = query.Where(b => b.Status == status);

            if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsedDate))
            {
                var startDate = parsedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var endDate = parsedDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                query = query.Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate);
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.Title.Contains(search) || b.BugNumber.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BugResponseDto>
            {
                Items      = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }


        private string MapBugStatusToWorkItemStatus(string bStatus)
        {
            return bStatus.ToLower() switch
            {
                "closed" or "resolved" => "completed",
                "fixed" => "fixed",
                "in_progress" => "in_progress",
                _ => "pending"
            };
        }

        public async Task<BugResponseDto?> UpdateBugStatusAsync(int bugId, UpdateBugStatusRequestDto request)
        {
            var bug = await _bugRepo.GetAsync(b => b.Id == bugId);
            if (bug == null) return null;

            bug.Status = request.Status;
            bug.UpdatedAt = DateTime.UtcNow;

            if (request.Status == "fixed")
            {
                bug.FixedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(request.FixedBuild))
                    bug.FixedBuild = request.FixedBuild;
            }

            if (request.Status == "closed" || request.Status == "resolved")
                bug.ClosedAt = DateTime.UtcNow;

            await _bugRepo.SaveAsync();

            // Sync status with linked WorkItem
            var workItem = await _workItemRepo.GetAsync(w => w.Id == bug.WorkItemId);
            if (workItem != null)
            {
                workItem.Status = MapBugStatusToWorkItemStatus(request.Status);
                workItem.UpdatedAt = DateTime.UtcNow;
                if (workItem.Status == "completed" || workItem.Status == "fixed")
                    workItem.CompletedAt = DateTime.UtcNow;
                await _workItemRepo.SaveAsync();
            }

            var updated = await _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .FirstOrDefaultAsync(b => b.Id == bugId);

            return MapToDto(updated!);
        }

        public async Task<BugResponseDto?> ReassignBugAsync(int bugId, ReassignBugRequestDto request)
        {
            var bug = await _bugRepo.GetAsync(b => b.Id == bugId);
            if (bug == null) return null;

            bug.AssignedToUserId = request.AssignedToUserId;
            bug.UpdatedAt = DateTime.UtcNow;

            await _bugRepo.SaveAsync();

            // Sync assignee with linked WorkItem
            var workItem = await _workItemRepo.GetAsync(w => w.Id == bug.WorkItemId);
            if (workItem != null)
            {
                workItem.AssignedToUserId = request.AssignedToUserId;
                workItem.UpdatedAt = DateTime.UtcNow;
                await _workItemRepo.SaveAsync();
            }

            var updated = await _bugRepo.Query()
                .Include(b => b.WorkItem)
                .Include(b => b.RaisedBy)
                .Include(b => b.AssignedTo)
                .FirstOrDefaultAsync(b => b.Id == bugId);

            return MapToDto(updated!);
        }

        private static BugResponseDto MapToDto(Bug bug)
        {
            return new BugResponseDto
            {
                Id = bug.Id,
                BugNumber = bug.BugNumber,
                Title = bug.Title,
                Description = bug.Description,
                ScreenshotUrl = bug.ScreenshotUrl,
                Status = bug.Status,
                WorkItemTitle = bug.WorkItem?.Title ?? string.Empty,
                WorkNumber = bug.WorkItem?.WorkNumber ?? string.Empty,
                WorkItemId = bug.WorkItemId,
                RaisedBy = bug.RaisedBy?.Name ?? string.Empty,
                AssignedTo = bug.AssignedTo?.Name,
                RaisedBuild = bug.RaisedBuild,
                FixedBuild = bug.FixedBuild,
                Severity = bug.Severity,
                IssueType = bug.IssueType,
                CreatedAt = bug.CreatedAt,
                FixedAt = bug.FixedAt,
                ClosedAt = bug.ClosedAt
            };
        }
    }
}