using System;
using System.Collections.Generic;

namespace TeamTrack.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductNumber { get; set; } = string.Empty; // PRD-001
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProjectId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Project? Project { get; set; }
        public ICollection<Module> Modules { get; set; } = [];
    }
}
