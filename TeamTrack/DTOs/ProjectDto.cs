namespace TeamTrack.DTOs
{
    public class CreateProjectRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<int> AssignedEmployeeIds { get; set; } = [];
        public int? ClientId { get; set; }
    }

    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public string ProjectNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<WorkItemResponseDto> WorkItems { get; set; } = [];
        public List<EmployeeDropdownDto> AssignedEmployees { get; set; } = [];
    }

    public class CreateWorkItemRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "medium";
        public string? Status { get; set; }
        public string? WorkType { get; set; }
        public DateTime? StartDate { get; set; }
        public int? ParentId { get; set; }
        public string? Labels { get; set; }
        public string? Team { get; set; }
        public string? AttachmentUrls { get; set; }
        public int? AssignedToUserId { get; set; }
        public DateTime? DueDate { get; set; }
        public int? ModuleId { get; set; }
    }

    public class WorkItemResponseDto
    {
        public int Id { get; set; }
        public string WorkNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string WorkType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public int? ParentId { get; set; }
        public string? ParentWorkNumber { get; set; }
        public string? ParentTitle { get; set; }
        public string? Labels { get; set; }
        public string? Team { get; set; }
        public string? AttachmentUrls { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectNumber { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int? ModuleId { get; set; }
        public string? ModuleName { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class UpdateWorkItemDueDateRequestDto
    {
        public DateTime? DueDate { get; set; }
    }

    public class EmployeeDropdownDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateWorkItemStatusRequestDto
    {
        public string Status { get; set; } = string.Empty; // pending, in_progress, completed, testing, bug_found, closed
    }

    public class ReassignWorkItemRequestDto
    {
        public int AssignedToUserId { get; set; }
    }

    public class EmployeeFullDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProjectMembersRequestDto
    {
        public List<int> AssignedEmployeeIds { get; set; } = [];
    }
}
