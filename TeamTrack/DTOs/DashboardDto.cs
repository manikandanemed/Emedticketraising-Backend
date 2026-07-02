namespace TeamTrack.DTOs
{
    public class DashboardResponseDto
    {
        public int TotalProjects { get; set; }
        public int TotalWorkItems { get; set; }
        public int TotalBugs { get; set; }

        public WorkItemStatusCountDto WorkItemStatusCount { get; set; } = new();
        public BugStatusCountDto BugStatusCount { get; set; } = new();
        public List<EmployeeWorkItemCountDto> EmployeeWorkItemCounts { get; set; } = [];
    }

    public class WorkItemStatusCountDto
    {
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Testing { get; set; }
        public int BugFound { get; set; }
        public int Closed { get; set; }
    }

    public class BugStatusCountDto
    {
        public int Open { get; set; }
        public int InProgress { get; set; }
        public int Fixed { get; set; }
        public int Closed { get; set; }
    }

    public class EmployeeWorkItemCountDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Testing { get; set; }
        public int BugFound { get; set; }
        public int Closed { get; set; }
        public List<EmployeeTaskDto> AssignedTasks { get; set; } = [];
        public string? LatestNote { get; set; }
        public DateTime? LatestNoteDate { get; set; }
        public List<DailyStatusNoteDto> NoteHistory { get; set; } = [];
    }

    public class EmployeeTaskDto
    {
        public int Id { get; set; }
        public string WorkNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public string? ProductName { get; set; }
        public string? ModuleName { get; set; }
    }

    public class DailyStatusNoteDto
    {
        public int Id { get; set; }
        public string NoteText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }
}