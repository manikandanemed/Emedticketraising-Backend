using System;

namespace TeamTrack.DTOs
{
    public class CreateTicketDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string Priority { get; set; } = "medium"; // low, medium, high, critical
        public int ProjectId { get; set; }
        public int? AssignedToUserId { get; set; }
        public int? BuildId { get; set; }
        public bool WhatsappNotify { get; set; } = true;
    }

    public class UpdateTicketDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string Priority { get; set; } = "medium";
        public string Status { get; set; } = "open"; // open, in_progress, resolved, closed
        public int? AssignedToUserId { get; set; }
        public int? BuildId { get; set; }
        public bool WhatsappNotify { get; set; } = true;
    }

    public class TicketResponseDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int RaisedByUserId { get; set; }
        public string RaisedByUserName { get; set; } = string.Empty;
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public int? BuildId { get; set; }
        public string? BuildNumber { get; set; }
        public bool WhatsappNotify { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
