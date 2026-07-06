using System;

namespace TeamTrack.Models
{
    public class SoftwareBuild
    {
        public int Id { get; set; }
        public string BuildNumber { get; set; } = string.Empty; // e.g. v1.0.0
        public int ProjectId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Project? Project { get; set; }
    }
}
