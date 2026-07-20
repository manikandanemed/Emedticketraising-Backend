using TeamTrack.DTOs;

namespace TeamTrack.Services
{
    public interface IProjectService
    {
        Task<ProjectResponseDto> CreateProjectAsync(CreateProjectRequestDto request, int userId);

        // Paginated projects list
        Task<PagedResult<ProjectResponseDto>> GetAllProjectsPagedAsync(int userId, string userRole, int page, int pageSize, string? search);

        // Keep non-paged for internal use (e.g., project detail + member modal)
        Task<List<ProjectResponseDto>> GetAllProjectsAsync(int userId, string userRole);

        Task<ProjectResponseDto?> GetProjectByIdAsync(int projectId);
        Task<WorkItemResponseDto> CreateWorkItemAsync(int projectId, CreateWorkItemRequestDto request, int userId);

        // Paginated project work items
        Task<PagedResult<WorkItemResponseDto>> GetWorkItemsByProjectPagedAsync(int projectId, int page, int pageSize, string? status, string? search, string? assignedTo = null, string? dueDate = null);

        // Non-paged for kanban/detail views. Returns null if the caller (an Employee)
        // is not assigned to this project — ProductManagers can access any project.
        Task<List<WorkItemResponseDto>?> GetWorkItemsByProjectAsync(int projectId, int userId, string userRole);

        // Paginated employee work items
        Task<PagedResult<WorkItemResponseDto>> GetWorkItemsByEmployeePagedAsync(int userId, int page, int pageSize, string? status, string? dueDate, string? search, string? workType = null, string? priority = null);

        // Non-paged (kept for backward compatibility if needed)
        Task<List<WorkItemResponseDto>> GetWorkItemsByEmployeeAsync(int userId);

        Task<List<EmployeeDropdownDto>> GetAllEmployeesAsync();
        Task<WorkItemResponseDto?> UpdateWorkItemStatusAsync(int workItemId, UpdateWorkItemStatusRequestDto request, int byUserId = 0);
        Task<WorkItemResponseDto?> ReassignWorkItemAsync(int workItemId, ReassignWorkItemRequestDto request, int byUserId = 0);
        Task<WorkItemResponseDto?> GetWorkItemByIdAsync(int workItemId);
        Task<WorkItemResponseDto?> UpdateWorkItemDueDateAsync(int workItemId, UpdateWorkItemDueDateRequestDto request);
        Task<bool> DeleteProjectAsync(int projectId);
        Task<List<EmployeeFullDto>> GetAllEmployeesFullAsync();
        Task<ProjectResponseDto?> UpdateProjectMembersAsync(int projectId, UpdateProjectMembersRequestDto request);
        Task<ProjectResponseDto?> UpdateProjectAsync(int projectId, UpdateProjectRequestDto request);
        Task<List<WorkItemActivityLogDto>> GetWorkItemActivityAsync(int workItemId);
        Task<List<WorkItemResponseDto>> GetInvolvedWorkItemsAsync(int userId);
    }
}

