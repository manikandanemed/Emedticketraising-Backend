namespace TeamTrack.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string UserType { get; set; } = "Employee"; // ProductManager, Employee
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ProfilePicture { get; set; }

        public ICollection<Ticket> RaisedTickets { get; set; } = [];
        public ICollection<Ticket> AssignedTickets { get; set; } = [];
        public ICollection<Comment> Comments { get; set; } = [];
        public ICollection<Project> AssignedProjects { get; set; } = [];
    }

    public class Ticket
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string Priority { get; set; } = "medium"; // low, medium, high, critical
        public string Status { get; set; } = "open"; // open, in_progress, resolved, closed
        public int ProjectId { get; set; }
        public int RaisedByUserId { get; set; }
        public int? AssignedToUserId { get; set; }
        public int? BuildId { get; set; }
        public bool WhatsappNotify { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public Project? Project { get; set; }
        public User? RaisedBy { get; set; }
        public User? AssignedTo { get; set; }
        public SoftwareBuild? Build { get; set; }
        public ICollection<Comment> Comments { get; set; } = [];
    }

    public class Comment
    {
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public WorkItem? WorkItem { get; set; }
        public User? User { get; set; }
    }

    public class Project
    {
        public int Id { get; set; }
        public string ProjectNumber { get; set; } = string.Empty; // PRJ-001
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "active"; // active, completed, cancelled
        public int CreatedByUserId { get; set; }
        public int? ClientId { get; set; } // Linked Client
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Client? Client { get; set; }
        public User? CreatedBy { get; set; }
        public ICollection<WorkItem> WorkItems { get; set; } = [];
        public ICollection<User> AssignedEmployees { get; set; } = [];
    }

    public class WorkItem
    {
        public int Id { get; set; }
        public string WorkNumber { get; set; } = string.Empty; // WRK-001
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "pending"; // pending, in_progress, completed
        public string Priority { get; set; } = "medium"; // low, medium, high
        public string WorkType { get; set; } = "Task"; // Epic, Task, Functional Requirements, Design Update, Bug
        public DateTime? StartDate { get; set; }
        public int? ParentId { get; set; }
        public string? Labels { get; set; }
        public string? Team { get; set; }
        public string? AttachmentUrls { get; set; }
        public string? EpicName { get; set; }
        public string? EpicColor { get; set; }
        public int ProjectId { get; set; }
        public int? ModuleId { get; set; } // Linked Module
        public int CreatedByUserId { get; set; }
        public int? AssignedToUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public string? FixedBillNumber { get; set; }
        public string? RaisedBillNumber { get; set; }
        public bool DeveloperBillLock { get; set; } = false;

        public Project? Project { get; set; }
        public Module? Module { get; set; }
        public User? CreatedBy { get; set; }
        public User? AssignedTo { get; set; }
    }

    public class Bug
    {
        public int Id { get; set; }
        public string BugNumber { get; set; } = string.Empty; // BUG-001
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ScreenshotUrl { get; set; }
        public string Status { get; set; } = "open"; // open, in_progress, fixed, closed
        public int WorkItemId { get; set; }
        public int RaisedByUserId { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? RaisedBuild { get; set; }
        public string? FixedBuild { get; set; }
        public string? Severity { get; set; }
        public string? IssueType { get; set; } = "New"; // New, Reopen, Regression
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FixedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public WorkItem? WorkItem { get; set; }
        public User? RaisedBy { get; set; }
        public User? AssignedTo { get; set; }
    }

    public class DailyStatusNote
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int CreatedByUserId { get; set; }
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? Employee { get; set; }
        public User? CreatedBy { get; set; }
    }

    public class PersonalNote
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime NoteDate { get; set; } = DateTime.UtcNow.Date;
        public string Priority { get; set; } = "medium";
        public int? AssignedToUserId { get; set; }

        public User? User { get; set; }
        public User? AssignedTo { get; set; }
    }

    /// <summary>
    /// Tracks every assignment, reassignment, and status change on a WorkItem (like Jira activity log)
    /// </summary>
    public class WorkItemActivityLog
    {
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        /// <summary>Assigned | Reassigned | StatusChanged | Created</summary>
        public string Action { get; set; } = string.Empty;
        public int? FromUserId { get; set; }
        public int? ToUserId { get; set; }
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public int ByUserId { get; set; }
        public string? Note { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public WorkItem? WorkItem { get; set; }
        public User? FromUser { get; set; }
        public User? ToUser { get; set; }
        public User? ByUser { get; set; }
    }

    public class UserOtp
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty; // Login or ResetPassword
        public DateTime ExpiryTime { get; set; } = DateTime.UtcNow.AddMinutes(10);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
