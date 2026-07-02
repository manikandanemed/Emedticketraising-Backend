using TeamTrack.DTOs;

namespace TeamTrack.Services
{
    public interface IBugService
    {
        Task<BugResponseDto> CreateBugAsync(CreateBugRequestDto request, int userId);
        Task<BugResponseDto> CreateBugWithScreenshotAsync(CreateBugRequestDto request, IFormFile? screenshot, int userId);
        Task<List<BugResponseDto>> GetBugsByWorkItemAsync(int workItemId);
        Task<List<BugResponseDto>> GetBugsByAssignedEmployeeAsync(int userId);
        Task<PagedResult<BugResponseDto>> GetBugsByAssignedEmployeePagedAsync(int userId, int page, int pageSize, string? status, string? date, string? search);
        Task<List<BugResponseDto>> GetAllBugsAsync();
        Task<PagedResult<BugResponseDto>> GetAllBugsPagedAsync(int page, int pageSize, string? status, string? date, string? search);
        Task<BugResponseDto?> UpdateBugStatusAsync(int bugId, UpdateBugStatusRequestDto request);
        Task<BugResponseDto?> ReassignBugAsync(int bugId, ReassignBugRequestDto request);
    }
}

