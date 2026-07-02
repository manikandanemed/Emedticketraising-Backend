using System;
using System.Collections.Generic;

namespace TeamTrack.Models
{
    public class Module
    {
        public int Id { get; set; }
        public string ModuleNumber { get; set; } = string.Empty; // MDL-001
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Product? Product { get; set; }
        public ICollection<WorkItem> WorkItems { get; set; } = [];
    }
}
