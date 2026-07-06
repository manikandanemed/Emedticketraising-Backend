using System;

namespace TeamTrack.DTOs
{
    public class SoftwareBuildDto
    {
        public int Id { get; set; }
        public string BuildNumber { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSoftwareBuildRequestDto
    {
        public string BuildNumber { get; set; } = string.Empty;
        public int ProjectId { get; set; }
    }
}
