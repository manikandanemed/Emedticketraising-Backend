namespace TeamTrack.DTOs
{
    public class CreateBugRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ScreenshotUrl { get; set; }
        public int WorkItemId { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? RaisedBuild { get; set; }
        public string? Severity { get; set; }
        public string? IssueType { get; set; }
    }

    public class UpdateBugStatusRequestDto
    {
        public string Status { get; set; } = string.Empty; // open, in_progress, fixed, closed
        public string? FixedBuild { get; set; }
    }

    public class ReassignBugRequestDto
    {
        public int AssignedToUserId { get; set; }
    }

    public class BugResponseDto
    {
        public int Id { get; set; }
        public string BugNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ScreenshotUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public string WorkItemTitle { get; set; } = string.Empty;
        public string WorkNumber { get; set; } = string.Empty;
        public int WorkItemId { get; set; }
        public string RaisedBy { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
        public string? RaisedBuild { get; set; }
        public string? FixedBuild { get; set; }
        public string? Severity { get; set; }
        public string? IssueType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FixedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
