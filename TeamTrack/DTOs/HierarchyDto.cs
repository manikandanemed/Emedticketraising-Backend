using System;
using System.Collections.Generic;

namespace TeamTrack.DTOs
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string ClientNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateClientRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProductRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProjectId { get; set; }
    }

    public class ModuleDto
    {
        public int Id { get; set; }
        public string ModuleNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateModuleRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductId { get; set; }
    }

    public class UpdateClientRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateProductRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProjectId { get; set; }
    }

    public class UpdateModuleRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductId { get; set; }
    }
}
