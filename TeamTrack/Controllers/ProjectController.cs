using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTrack.DTOs;
using TeamTrack.Services;
using Microsoft.EntityFrameworkCore;
using TeamTrack.Data;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IBugService _bugService;
        private readonly ICommentService _commentService;
        private readonly IAuthService _authService;
        private readonly IFileService _fileService;

        public ProjectController(
            IProjectService projectService, 
            IBugService bugService, 
            ICommentService commentService, 
            IAuthService authService, 
            IFileService fileService)
        {
            _projectService = projectService;
            _bugService = bugService;
            _commentService = commentService;
            _authService = authService;
            _fileService = fileService;
        }

        // ==================== PROJECT ====================

        [HttpPost]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Name))
                return BadRequest(ApiResponse<string>.FailureResponse("Project name is required"));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _projectService.CreateProjectAsync(request, userId);
            return Ok(ApiResponse<ProjectResponseDto>.SuccessResponse(result, "Project created successfully"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProjects()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Employee";
            var result = await _projectService.GetAllProjectsAsync(userId, userRole);
            return Ok(ApiResponse<List<ProjectResponseDto>>.SuccessResponse(result, "Projects fetched successfully"));
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetAllProjectsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var userId   = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Employee";
            var result   = await _projectService.GetAllProjectsPagedAsync(userId, userRole, page, pageSize, search);
            return Ok(ApiResponse<PagedResult<ProjectResponseDto>>.SuccessResponse(result, "Projects fetched successfully"));
        }

        [HttpGet("{projectId}")]
        public async Task<IActionResult> GetProjectById(int projectId)
        {
            var result = await _projectService.GetProjectByIdAsync(projectId);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found"));

            return Ok(ApiResponse<ProjectResponseDto>.SuccessResponse(result, "Project fetched successfully"));
        }

        [HttpPut("{projectId}/members")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> UpdateProjectMembers(int projectId, [FromBody] UpdateProjectMembersRequestDto request)
        {
            var result = await _projectService.UpdateProjectMembersAsync(projectId, request);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found"));

            return Ok(ApiResponse<ProjectResponseDto>.SuccessResponse(result, "Project members updated successfully"));
        }

        // ==================== WORK ITEMS ====================

        [HttpPost("{projectId}/workitems")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> CreateWorkItem
            (int projectId, [FromBody] CreateWorkItemRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Title))
                return BadRequest(ApiResponse<string>.FailureResponse("Title is required"));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _projectService.CreateWorkItemAsync(projectId, request, userId);
            return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result, "Work item created successfully"));
        }

        [HttpPost("workitems/upload-attachment")]
        public async Task<IActionResult> UploadAttachment(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<string>.FailureResponse("No file uploaded"));

            try
            {
                var fileUrl = await _fileService.UploadScreenshotAsync(file);
                return Ok(ApiResponse<string>.SuccessResponse(fileUrl, "File uploaded successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.FailureResponse(ex.Message));
            }
        }

        [HttpGet("{projectId}/workitems")]
        public async Task<IActionResult> GetWorkItemsByProject(int projectId)
        {
            var result = await _projectService.GetWorkItemsByProjectAsync(projectId);
            return Ok(ApiResponse<List<WorkItemResponseDto>>.SuccessResponse(result, "Work items fetched successfully"));
        }

        [HttpGet("{projectId}/workitems/paged")]
        public async Task<IActionResult> GetWorkItemsByProjectPaged(
            int projectId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            var result = await _projectService.GetWorkItemsByProjectPagedAsync(projectId, page, pageSize, status, search);
            return Ok(ApiResponse<PagedResult<WorkItemResponseDto>>.SuccessResponse(result, "Work items fetched successfully"));
        }

        [HttpGet("workitems/myworks")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyWorkItems()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _projectService.GetWorkItemsByEmployeeAsync(userId);
            return Ok(ApiResponse<List<WorkItemResponseDto>>.SuccessResponse(result, "Work items fetched successfully"));
        }

        [HttpGet("workitems/myworks/paged")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyWorkItemsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? dueDate = null,
            [FromQuery] string? search = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _projectService.GetWorkItemsByEmployeePagedAsync(userId, page, pageSize, status, dueDate, search);
            return Ok(ApiResponse<PagedResult<WorkItemResponseDto>>.SuccessResponse(result, "Work items fetched successfully"));
        }

        [HttpGet("employees/dropdown")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> GetEmployeesDropdown()
        {
            var result = await _projectService.GetAllEmployeesAsync();
            return Ok(ApiResponse<List<EmployeeDropdownDto>>.SuccessResponse(result, "Employees fetched successfully"));
        }

        // ==================== BUGS ====================

        [HttpPost("workitems/bugs")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CreateBug([FromForm] CreateBugRequestDto request, IFormFile? screenshot)
        {
            if (string.IsNullOrEmpty(request.Title))
                return BadRequest(ApiResponse<string>.FailureResponse("Title is required"));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bugService.CreateBugWithScreenshotAsync(request, screenshot, userId);
            return Ok(ApiResponse<BugResponseDto>.SuccessResponse(result, "Bug created successfully"));
        }

        [HttpGet("workitems/{workItemId}/bugs")]
        public async Task<IActionResult> GetBugsByWorkItem(int workItemId)
        {
            var result = await _bugService.GetBugsByWorkItemAsync(workItemId);
            return Ok(ApiResponse<List<BugResponseDto>>.SuccessResponse(result, "Bugs fetched successfully"));
        }

        [HttpGet("workitems/bugs/mybugs")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyBugs()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bugService.GetBugsByAssignedEmployeeAsync(userId);
            return Ok(ApiResponse<List<BugResponseDto>>.SuccessResponse(result, "Bugs fetched successfully"));
        }

        [HttpGet("workitems/bugs/mybugs/paged")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyBugsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? date = null,
            [FromQuery] string? search = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bugService.GetBugsByAssignedEmployeePagedAsync(userId, page, pageSize, status, date, search);
            return Ok(ApiResponse<PagedResult<BugResponseDto>>.SuccessResponse(result, "Bugs fetched successfully"));
        }

        [HttpGet("workitems/bugs/all")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> GetAllBugs()
        {
            var result = await _bugService.GetAllBugsAsync();
            return Ok(ApiResponse<List<BugResponseDto>>.SuccessResponse(result, "Bugs fetched successfully"));
        }

        [HttpGet("workitems/bugs/all/paged")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> GetAllBugsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? date = null,
            [FromQuery] string? search = null)
        {
            var result = await _bugService.GetAllBugsPagedAsync(page, pageSize, status, date, search);
            return Ok(ApiResponse<PagedResult<BugResponseDto>>.SuccessResponse(result, "Bugs fetched successfully"));
        }


        [HttpPut("workitems/bugs/{bugId}/status")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> UpdateBugStatus(int bugId, [FromBody] UpdateBugStatusRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Status))
                return BadRequest(ApiResponse<string>.FailureResponse("Status is required"));

            var result = await _bugService.UpdateBugStatusAsync(bugId, request);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("Bug not found"));

            return Ok(ApiResponse<BugResponseDto>.SuccessResponse(result, "Bug status updated successfully"));
        }

        [HttpPut("workitems/bugs/{bugId}/reassign")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> ReassignBug(int bugId, [FromBody] ReassignBugRequestDto request)
        {
            var result = await _bugService.ReassignBugAsync(bugId, request);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("Bug not found"));

            return Ok(ApiResponse<BugResponseDto>.SuccessResponse(result, "Bug reassigned successfully"));
        }

        [HttpPut("workitems/{workItemId}/status")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> UpdateWorkItemStatus(int workItemId, [FromBody] UpdateWorkItemStatusRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Status))
                return BadRequest(ApiResponse<string>.FailureResponse("Status is required"));

            var result = await _projectService.UpdateWorkItemStatusAsync(workItemId, request);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("WorkItem not found"));

            return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result, "WorkItem status updated successfully"));
        }

        [HttpPut("workitems/{workItemId}/reassign")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> ReassignWorkItem(int workItemId, [FromBody] ReassignWorkItemRequestDto request)
        {
            var result = await _projectService.ReassignWorkItemAsync(workItemId, request);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("WorkItem not found"));

            return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result, "WorkItem reassigned successfully"));
        }

        [HttpPut("workitems/{workItemId}/duedate")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> UpdateWorkItemDueDate(int workItemId, [FromBody] UpdateWorkItemDueDateRequestDto request)
        {
            var result = await _projectService.UpdateWorkItemDueDateAsync(workItemId, request);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("WorkItem not found"));

            return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result, "WorkItem due date updated successfully"));
        }

        // ==================== COMMENTS ====================

        [HttpPost("workitems/comments")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> AddComment([FromBody] CreateCommentRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest(ApiResponse<string>.FailureResponse("Message is required"));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _commentService.AddCommentAsync(request, userId);
            return Ok(ApiResponse<CommentResponseDto>.SuccessResponse(result, "Comment added successfully"));
        }

        [HttpGet("workitems/{workItemId}/comments")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> GetCommentsByWorkItem(int workItemId)
        {
            var isProductManager = User.IsInRole("ProductManager");
            var result = await _commentService.GetCommentsByWorkItemAsync(workItemId, isProductManager);
            return Ok(ApiResponse<List<CommentResponseDto>>.SuccessResponse(result, "Comments fetched successfully"));
        }

        [HttpGet("workitems/{workItemId}")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> GetWorkItemById(int workItemId)
        {
            var result = await _projectService.GetWorkItemByIdAsync(workItemId);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("WorkItem not found"));

            return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result, "WorkItem fetched successfully"));
        }

        // ==================== EMPLOYEES MANAGEMENT ====================

        [HttpGet("employees")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> GetAllEmployeesFull()
        {
            var result = await _projectService.GetAllEmployeesFullAsync();
            return Ok(ApiResponse<List<EmployeeFullDto>>.SuccessResponse(result, "Employees fetched successfully"));
        }

        [HttpPut("employees/{userId}/deactivate")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> DeactivateEmployee(int userId)
        {
            var result = await _authService.DeactivateEmployeeAsync(userId);
            if (!result)
                return NotFound(ApiResponse<string>.FailureResponse("Employee not found"));

            return Ok(ApiResponse<string>.SuccessResponse("Deactivated", "Employee deactivated successfully"));
        }

        [HttpPost("employees/{userId}/reset-password")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> AdminResetPassword(int userId, [FromBody] AdminResetPasswordRequestDto request)
        {
            if (string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(ApiResponse<string>.FailureResponse("New password is required"));
            }

            var success = await _authService.AdminResetPasswordAsync(userId, request.NewPassword);
            if (!success)
            {
                return NotFound(ApiResponse<string>.FailureResponse("Employee not found"));
            }

            return Ok(ApiResponse<string>.SuccessResponse("Success", "Employee password reset successfully"));
        }

        [HttpPut("employees/{userId}/update-email")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> AdminUpdateEmail(int userId, [FromBody] AdminUpdateEmailRequestDto request, [FromServices] IRepository<User> userRepo)
        {
            if (string.IsNullOrEmpty(request.NewEmail))
                return BadRequest(ApiResponse<string>.FailureResponse("New email is required"));

            var emailLower = request.NewEmail.Trim().ToLower();

            // Check duplicate email
            var existing = await userRepo.GetAsync(u => u.Email.ToLower() == emailLower && u.Id != userId);
            if (existing != null)
                return Conflict(ApiResponse<string>.FailureResponse("This email is already in use by another user"));

            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user == null)
                return NotFound(ApiResponse<string>.FailureResponse("Employee not found"));

            user.Email = emailLower;
            await userRepo.SaveAsync();

            return Ok(ApiResponse<string>.SuccessResponse(emailLower, "Email updated successfully"));
        }

        [HttpDelete("{projectId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            var result = await _projectService.DeleteProjectAsync(projectId);
            if (!result)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found"));

            return Ok(ApiResponse<string>.SuccessResponse("Deleted", "Project deleted successfully"));
        }

        // ==================== PERSONAL NOTES ====================

        [HttpGet("personalnotes")]
        public async Task<IActionResult> GetPersonalNotes([FromQuery] string? date, [FromServices] AppDbContext context)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));
            int userId = int.Parse(userIdClaim.Value);

            var query = context.PersonalNotes.Where(n => n.UserId == userId);

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                var targetDateUtc = parsedDate.Date.ToUniversalTime();
                query = query.Where(n => n.NoteDate.Date == targetDateUtc.Date);
            }

            var notes = await query
                .OrderBy(n => n.Priority == "critical" ? 1 :
                              n.Priority == "high" ? 2 :
                              n.Priority == "medium" ? 3 : 4)
                .ThenByDescending(n => n.NoteDate)
                .ThenByDescending(n => n.CreatedAt)
                .Select(n => new PersonalNoteDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt,
                    NoteDate = n.NoteDate,
                    Priority = n.Priority
                })
                .ToListAsync();

            return Ok(ApiResponse<List<PersonalNoteDto>>.SuccessResponse(notes, "Personal notes fetched successfully."));
        }

        [HttpPost("personalnotes")]
        public async Task<IActionResult> CreatePersonalNote([FromBody] CreatePersonalNoteRequest request, [FromServices] AppDbContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Note content cannot be empty."));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));
            int userId = int.Parse(userIdClaim.Value);

            var noteDate = request.NoteDate ?? DateTime.UtcNow.Date;
            var priority = request.Priority ?? "medium";

            var validPriorities = new[] { "low", "medium", "high", "critical" };
            if (!validPriorities.Contains(priority.ToLower()))
            {
                priority = "medium";
            }

            var note = new PersonalNote
            {
                UserId = userId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                NoteDate = noteDate.Date.ToUniversalTime(),
                Priority = priority.ToLower()
            };

            context.PersonalNotes.Add(note);
            await context.SaveChangesAsync();

            var dto = new PersonalNoteDto
            {
                Id = note.Id,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                NoteDate = note.NoteDate,
                Priority = note.Priority
            };

            return Ok(ApiResponse<PersonalNoteDto>.SuccessResponse(dto, "Personal note created successfully."));
        }

        [HttpPut("personalnotes/{id}")]
        public async Task<IActionResult> UpdatePersonalNote(int id, [FromBody] CreatePersonalNoteRequest request, [FromServices] AppDbContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Note content cannot be empty."));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));
            int userId = int.Parse(userIdClaim.Value);

            var note = await context.PersonalNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (note == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Note not found."));
            }

            var noteDate = request.NoteDate ?? note.NoteDate;
            var priority = request.Priority ?? note.Priority;

            var validPriorities = new[] { "low", "medium", "high", "critical" };
            if (!validPriorities.Contains(priority.ToLower()))
            {
                priority = "medium";
            }

            note.Content = request.Content;
            note.NoteDate = noteDate.Date.ToUniversalTime();
            note.Priority = priority.ToLower();

            await context.SaveChangesAsync();

            var dto = new PersonalNoteDto
            {
                Id = note.Id,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                NoteDate = note.NoteDate,
                Priority = note.Priority
            };

            return Ok(ApiResponse<PersonalNoteDto>.SuccessResponse(dto, "Personal note updated successfully."));
        }

        [HttpDelete("personalnotes/{id}")]
        public async Task<IActionResult> DeletePersonalNote(int id, [FromServices] AppDbContext context)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));
            int userId = int.Parse(userIdClaim.Value);

            var note = await context.PersonalNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (note == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Note not found."));
            }

            context.PersonalNotes.Remove(note);
            await context.SaveChangesAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Note deleted successfully."));
        }

        // ==================== HIERARCHY MANAGEMENT (CLIENT - PRODUCT - MODULE) ====================

        [HttpGet("clients")]
        public async Task<IActionResult> GetClients([FromServices] IRepository<Client> clientRepo)
        {
            var clients = await clientRepo.Query()
                .OrderBy(c => c.Name)
                .Select(c => new ClientDto
                {
                    Id = c.Id,
                    ClientNumber = c.ClientNumber,
                    Name = c.Name,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ClientDto>>.SuccessResponse(clients, "Clients fetched successfully"));
        }

        [HttpPost("clients")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientRequestDto request, [FromServices] IRepository<Client> clientRepo)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse<string>.FailureResponse("Client name is required"));

            var count = await clientRepo.Query().CountAsync();
            var client = new Client
            {
                ClientNumber = $"CLT-{(count + 1):D3}",
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await clientRepo.AddAsync(client);
            await clientRepo.SaveAsync();

            var dto = new ClientDto
            {
                Id = client.Id,
                ClientNumber = client.ClientNumber,
                Name = client.Name,
                Description = client.Description,
                CreatedAt = client.CreatedAt
            };

            return Ok(ApiResponse<ClientDto>.SuccessResponse(dto, "Client created successfully"));
        }

        [HttpGet("{projectId}/products")]
        public async Task<IActionResult> GetProducts(int projectId, [FromServices] IRepository<Product> productRepo)
        {
            var products = await productRepo.Query()
                .Include(p => p.Project)
                .Where(p => p.ProjectId == projectId)
                .OrderBy(p => p.Name)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    ProductNumber = p.ProductNumber,
                    Name = p.Name,
                    Description = p.Description,
                    ProjectId = p.ProjectId,
                    ProjectName = p.Project != null ? p.Project.Name : string.Empty,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ProductDto>>.SuccessResponse(products, "Products fetched successfully"));
        }

        [HttpPost("products")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto request, [FromServices] IRepository<Product> productRepo, [FromServices] IRepository<Project> projectRepo)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse<string>.FailureResponse("Product name is required"));

            var projectExists = await projectRepo.Query().AnyAsync(p => p.Id == request.ProjectId);
            if (!projectExists)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found"));

            var count = await productRepo.Query().CountAsync();
            var product = new Product
            {
                ProductNumber = $"PRD-{(count + 1):D3}",
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                ProjectId = request.ProjectId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await productRepo.AddAsync(product);
            await productRepo.SaveAsync();

            var created = await productRepo.Query()
                .Include(p => p.Project)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            var dto = new ProductDto
            {
                Id = created!.Id,
                ProductNumber = created.ProductNumber,
                Name = created.Name,
                Description = created.Description,
                ProjectId = created.ProjectId,
                ProjectName = created.Project?.Name ?? string.Empty,
                CreatedAt = created.CreatedAt
            };

            return Ok(ApiResponse<ProductDto>.SuccessResponse(dto, "Product created successfully"));
        }

        [HttpGet("products/{productId}/modules")]
        public async Task<IActionResult> GetModules(int productId, [FromServices] IRepository<Module> moduleRepo)
        {
            var modules = await moduleRepo.Query()
                .Include(m => m.Product)
                .Where(m => m.ProductId == productId)
                .OrderBy(m => m.Name)
                .Select(m => new ModuleDto
                {
                    Id = m.Id,
                    ModuleNumber = m.ModuleNumber,
                    Name = m.Name,
                    Description = m.Description,
                    ProductId = m.ProductId,
                    ProductName = m.Product != null ? m.Product.Name : string.Empty,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<ModuleDto>>.SuccessResponse(modules, "Modules fetched successfully"));
        }

        [HttpPost("modules")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequestDto request, [FromServices] IRepository<Module> moduleRepo, [FromServices] IRepository<Product> productRepo)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse<string>.FailureResponse("Module name is required"));

            var productExists = await productRepo.Query().AnyAsync(p => p.Id == request.ProductId);
            if (!productExists)
                return NotFound(ApiResponse<string>.FailureResponse("Product not found"));

            var count = await moduleRepo.Query().CountAsync();
            var module = new Module
            {
                ModuleNumber = $"MDL-{(count + 1):D3}",
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                ProductId = request.ProductId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await moduleRepo.AddAsync(module);
            await moduleRepo.SaveAsync();

            var created = await moduleRepo.Query()
                .Include(m => m.Product)
                .FirstOrDefaultAsync(m => m.Id == module.Id);

            var dto = new ModuleDto
            {
                Id = created!.Id,
                ModuleNumber = created.ModuleNumber,
                Name = created.Name,
                Description = created.Description,
                ProductId = created.ProductId,
                ProductName = created.Product?.Name ?? string.Empty,
                CreatedAt = created.CreatedAt
            };

            return Ok(ApiResponse<ModuleDto>.SuccessResponse(dto, "Module created successfully"));
        }
    }

    public class PersonalNoteDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime NoteDate { get; set; }
        public string Priority { get; set; } = "medium";
    }

    public class CreatePersonalNoteRequest
    {
        public string Content { get; set; } = string.Empty;
        public DateTime? NoteDate { get; set; }
        public string? Priority { get; set; }
    }
}