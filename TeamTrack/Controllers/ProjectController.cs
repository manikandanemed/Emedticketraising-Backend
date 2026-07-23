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

        [HttpPut("{projectId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> UpdateProject(int projectId, [FromBody] UpdateProjectRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Name))
                return BadRequest(ApiResponse<string>.FailureResponse("Project name is required"));

            var result = await _projectService.UpdateProjectAsync(projectId, request);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found"));

            return Ok(ApiResponse<ProjectResponseDto>.SuccessResponse(result, "Project updated successfully"));
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
            try
            {
                var result = await _projectService.CreateWorkItemAsync(projectId, request, userId);
                return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result, "Work item created successfully"));
            }
            catch (Exception ex)
            {
                var messages = new List<string>();
                Exception? current = ex;
                while (current != null)
                {
                    if (!string.IsNullOrWhiteSpace(current.Message))
                    {
                        messages.Add(current.Message);
                    }
                    current = current.InnerException;
                }
                var detail = string.Join(" -> ", messages.Distinct());
                return StatusCode(500, ApiResponse<string>.FailureResponse(detail));
            }
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
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Employee";
            var result = await _projectService.GetWorkItemsByProjectAsync(projectId, userId, userRole);
            if (result == null)
                return StatusCode(403, ApiResponse<string>.FailureResponse("You don't have access to this project"));

            return Ok(ApiResponse<List<WorkItemResponseDto>>.SuccessResponse(result, "Work items fetched successfully"));
        }

        [HttpGet("{projectId}/workitems/paged")]
        public async Task<IActionResult> GetWorkItemsByProjectPaged(
            int projectId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] string? assignedTo = null,
            [FromQuery] string? dueDate = null)
        {
            var result = await _projectService.GetWorkItemsByProjectPagedAsync(projectId, page, pageSize, status, search, assignedTo, dueDate);
            return Ok(ApiResponse<PagedResult<WorkItemResponseDto>>.SuccessResponse(result, "Work items fetched successfully"));
        }


        [HttpGet("workitems/myworks/paged")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyWorkItemsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? dueDate = null,
            [FromQuery] string? search = null,
            [FromQuery] string? workType = null,
            [FromQuery] string? priority = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _projectService.GetWorkItemsByEmployeePagedAsync(userId, page, pageSize, status, dueDate, search, workType, priority);
            return Ok(ApiResponse<PagedResult<WorkItemResponseDto>>.SuccessResponse(result, "Work items fetched successfully"));
        }

        [HttpGet("workitems/myworks/involved")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> GetInvolvedWorkItems()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _projectService.GetInvolvedWorkItemsAsync(userId);
            return Ok(ApiResponse<List<WorkItemResponseDto>>.SuccessResponse(result, "Involved work items fetched successfully"));
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
            try
            {
                var result = await _bugService.CreateBugWithScreenshotAsync(request, screenshot, userId);
                return Ok(ApiResponse<BugResponseDto>.SuccessResponse(result, "Bug created successfully"));
            }
            catch (Exception ex)
            {
                var messages = new List<string>();
                Exception? current = ex;
                while (current != null)
                {
                    if (!string.IsNullOrWhiteSpace(current.Message))
                    {
                        messages.Add(current.Message);
                    }
                    current = current.InnerException;
                }
                var detail = string.Join(" -> ", messages.Distinct());
                return StatusCode(500, ApiResponse<string>.FailureResponse(detail));
            }
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
        public async Task<IActionResult> UpdateWorkItemStatus(
            int workItemId,
            [FromBody] UpdateWorkItemStatusRequestDto request,
            [FromServices] IRepository<SoftwareBuild> buildRepo,
            [FromServices] IRepository<WorkItem> workItemRepo)
        {
            if (string.IsNullOrEmpty(request.Status))
                return BadRequest(ApiResponse<string>.FailureResponse("Status is required"));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Auto-save custom FixedBuild to software_builds table if not already present
            if (!string.IsNullOrWhiteSpace(request.FixedBuild))
            {
                var workItem = await workItemRepo.GetAsync(w => w.Id == workItemId);
                if (workItem != null)
                {
                    var alreadyExists = await buildRepo.Query()
                        .AnyAsync(b => b.ProjectId == workItem.ProjectId &&
                                       b.BuildNumber.ToLower() == request.FixedBuild.Trim().ToLower());
                    if (!alreadyExists)
                    {
                        var newBuild = new SoftwareBuild
                        {
                            BuildNumber = request.FixedBuild.Trim(),
                            ProjectId = workItem.ProjectId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await buildRepo.AddAsync(newBuild);
                        await buildRepo.SaveAsync();
                    }
                }
            }

            var result = await _projectService.UpdateWorkItemStatusAsync(workItemId, request, userId);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("WorkItem not found"));

            return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result, "WorkItem status updated successfully"));
        }

        [HttpPut("workitems/{workItemId}/reassign")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> ReassignWorkItem(int workItemId, [FromBody] ReassignWorkItemRequestDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _projectService.ReassignWorkItemAsync(workItemId, request, userId);
            if (result == null)
                return NotFound(ApiResponse<string>.FailureResponse("WorkItem not found"));

            return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result, "WorkItem reassigned successfully"));
        }

        [HttpGet("workitems/{workItemId}/activity")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> GetWorkItemActivity(int workItemId)
        {
            var logs = await _projectService.GetWorkItemActivityAsync(workItemId);
            return Ok(ApiResponse<List<WorkItemActivityLogDto>>.SuccessResponse(logs, "Activity log fetched"));
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

        [HttpPut("workitems/{workItemId}")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> UpdateWorkItem(int workItemId, [FromBody] CreateWorkItemRequestDto request, [FromServices] IRepository<WorkItem> workItemRepo)
        {
            if (string.IsNullOrEmpty(request.Title))
                return BadRequest(ApiResponse<string>.FailureResponse("Title is required"));

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Employee";

            var workItem = await workItemRepo.GetAsync(w => w.Id == workItemId);
            if (workItem == null)
                return NotFound(ApiResponse<string>.FailureResponse("Task not found"));

            if (userRole != "ProductManager" && workItem.CreatedByUserId != userId)
                return StatusCode(403, ApiResponse<string>.FailureResponse("Only the creator of this task can edit it"));

            workItem.Title = request.Title.Trim();
            workItem.Description = request.Description?.Trim();
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var isBugItem = workItem.WorkType.Equals("Bug", StringComparison.OrdinalIgnoreCase);
                workItem.Status = ProjectService.ResolveWorkItemStatus(request.Status, isBugItem);
            }
            workItem.Priority = request.Priority;
            workItem.ModuleId = request.ModuleId;
            workItem.StartDate = request.StartDate;
            workItem.DueDate = request.DueDate;
            workItem.Labels = request.Labels;
            workItem.Team = request.Team;
            workItem.ParentId = request.ParentId;
            workItem.EpicName = request.EpicName;
            workItem.EpicColor = request.EpicColor;
            workItem.RaisedBuild = request.RaisedBuild;
            workItem.FixedBuild = request.FixedBuild;
            workItem.AssignedToUserId = request.AssignedToUserId;
            workItem.AttachmentUrls = request.AttachmentUrls ?? workItem.AttachmentUrls;
            workItem.FixedBillNumber = request.FixedBillNumber ?? workItem.FixedBillNumber;
            workItem.RaisedBillNumber = request.RaisedBillNumber ?? workItem.RaisedBillNumber;
            if (request.DeveloperBillLock) workItem.DeveloperBillLock = request.DeveloperBillLock;
            workItem.UpdatedAt = DateTime.UtcNow;

            await workItemRepo.SaveAsync();

            var bugRepo = HttpContext.RequestServices.GetRequiredService<IRepository<Bug>>();
            var bug = await bugRepo.GetAsync(b => b.WorkItemId == workItemId);
            if (bug != null)
            {
                bug.Title = workItem.Title;
                bug.Description = workItem.Description;
                bug.Severity = request.Severity ?? bug.Severity;
                bug.IssueType = request.IssueType ?? bug.IssueType;
                bug.RaisedBuild = request.RaisedBuild;
                bug.FixedBuild = request.FixedBuild;
                bug.UpdatedAt = DateTime.UtcNow;
                await bugRepo.SaveAsync();
            }
            else if (!string.IsNullOrEmpty(request.RaisedBuild) || !string.IsNullOrEmpty(request.FixedBuild))
            {
                bug = new Bug
                {
                    BugNumber = workItem.WorkNumber,
                    Title = workItem.Title,
                    Description = workItem.Description,
                    Status = "New",
                    WorkItemId = workItem.Id,
                    RaisedByUserId = userId,
                    AssignedToUserId = workItem.AssignedToUserId,
                    Severity = request.Severity ?? "3",
                    IssueType = request.IssueType ?? "New",
                    RaisedBuild = request.RaisedBuild,
                    FixedBuild = request.FixedBuild,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await bugRepo.AddAsync(bug);
                await bugRepo.SaveAsync();
            }

            var result = await _projectService.GetWorkItemByIdAsync(workItemId);
            return Ok(ApiResponse<WorkItemResponseDto>.SuccessResponse(result!, "Task updated successfully"));
        }

        [HttpDelete("workitems/{workItemId}")]
        [Authorize(Roles = "Employee,ProductManager")]
        public async Task<IActionResult> DeleteWorkItem(int workItemId, [FromServices] IRepository<WorkItem> workItemRepo)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Employee";

            var workItem = await workItemRepo.GetAsync(w => w.Id == workItemId);
            if (workItem == null)
                return NotFound(ApiResponse<string>.FailureResponse("Task not found"));

            if (userRole != "ProductManager" && workItem.CreatedByUserId != userId)
                return StatusCode(403, ApiResponse<string>.FailureResponse("Only the creator of this task can delete it"));

            workItemRepo.Remove(workItem);
            await workItemRepo.SaveAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Deleted", "Task deleted successfully"));
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

            var query = context.PersonalNotes
                .Include(n => n.AssignedTo)
                .Include(n => n.User)
                .Where(n => n.UserId == userId || n.AssignedToUserId == userId);

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                var targetDateUtc = parsedDate.Date.ToUniversalTime();
                query = query.Where(n => n.NoteDate.Date == targetDateUtc.Date);
            }

            var notes = await query
                .OrderBy(n => n.Priority == "critical" ? 1 :
                              n.Priority == "high" ? 2 :
                              n.Priority == "medium" ? 3 : 4)
                .ThenBy(n => n.NoteDate)
                .ThenBy(n => n.CreatedAt)
                .Select(n => new PersonalNoteDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt,
                    NoteDate = n.NoteDate,
                    Priority = n.Priority,
                    AssignedToUserId = n.AssignedToUserId,
                    AssignedToUserName = n.AssignedTo != null ? n.AssignedTo.Name : null,
                    CreatorUserId = n.UserId,
                    CreatorUserName = n.User != null ? n.User.Name : null
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
                Priority = priority.ToLower(),
                AssignedToUserId = request.AssignedToUserId
            };

            context.PersonalNotes.Add(note);
            await context.SaveChangesAsync();

            // Load assignee name if set
            string? assignedToName = null;
            if (note.AssignedToUserId.HasValue)
            {
                var assignedUser = await context.Users.FindAsync(note.AssignedToUserId.Value);
                assignedToName = assignedUser?.Name;
            }

            var creatorUser = await context.Users.FindAsync(note.UserId);
            var dto = new PersonalNoteDto
            {
                Id = note.Id,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                NoteDate = note.NoteDate,
                Priority = note.Priority,
                AssignedToUserId = note.AssignedToUserId,
                AssignedToUserName = assignedToName,
                CreatorUserId = note.UserId,
                CreatorUserName = creatorUser?.Name
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
            note.AssignedToUserId = request.AssignedToUserId;

            await context.SaveChangesAsync();

            // Load assignee name if set
            string? assignedToName = null;
            if (note.AssignedToUserId.HasValue)
            {
                var assignedUser = await context.Users.FindAsync(note.AssignedToUserId.Value);
                assignedToName = assignedUser?.Name;
            }

            var creatorUserObj = await context.Users.FindAsync(note.UserId);
            var dtoUpdate = new PersonalNoteDto
            {
                Id = note.Id,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                NoteDate = note.NoteDate,
                Priority = note.Priority,
                AssignedToUserId = note.AssignedToUserId,
                AssignedToUserName = assignedToName,
                CreatorUserId = note.UserId,
                CreatorUserName = creatorUserObj?.Name
            };

            return Ok(ApiResponse<PersonalNoteDto>.SuccessResponse(dtoUpdate, "Personal note updated successfully."));
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

        [HttpGet("{projectId}/builds")]
        public async Task<IActionResult> GetBuilds(int projectId, [FromServices] IRepository<SoftwareBuild> buildRepo)
        {
            var builds = await buildRepo.Query()
                .Include(b => b.Project)
                .Where(b => b.ProjectId == projectId && b.IsActive)
                .OrderBy(b => b.CreatedAt)
                .Select(b => new SoftwareBuildDto
                {
                    Id = b.Id,
                    BuildNumber = b.BuildNumber,
                    ProjectId = b.ProjectId,
                    ProjectName = b.Project != null ? b.Project.Name : string.Empty,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<SoftwareBuildDto>>.SuccessResponse(builds, "Builds fetched successfully"));
        }

        [HttpPost("builds")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> CreateBuild([FromBody] CreateSoftwareBuildRequestDto request, [FromServices] IRepository<SoftwareBuild> buildRepo, [FromServices] IRepository<Project> projectRepo)
        {
            if (string.IsNullOrWhiteSpace(request.BuildNumber))
                return BadRequest(ApiResponse<string>.FailureResponse("Build number is required"));

            var projectExists = await projectRepo.Query().AnyAsync(p => p.Id == request.ProjectId);
            if (!projectExists)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found"));

            var exists = await buildRepo.Query().AnyAsync(b => b.ProjectId == request.ProjectId && b.BuildNumber.ToLower() == request.BuildNumber.Trim().ToLower());
            if (exists)
                return BadRequest(ApiResponse<string>.FailureResponse("Build number already exists for this project"));

            var build = new SoftwareBuild
            {
                BuildNumber = request.BuildNumber.Trim(),
                ProjectId = request.ProjectId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await buildRepo.AddAsync(build);
            await buildRepo.SaveAsync();

            var created = await buildRepo.Query()
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.Id == build.Id);

            var dto = new SoftwareBuildDto
            {
                Id = created!.Id,
                BuildNumber = created.BuildNumber,
                ProjectId = created.ProjectId,
                ProjectName = created.Project?.Name ?? string.Empty,
                IsActive = created.IsActive,
                CreatedAt = created.CreatedAt
            };

            return Ok(ApiResponse<SoftwareBuildDto>.SuccessResponse(dto, "Build created successfully"));
        }

        [HttpDelete("builds/{buildId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> DeleteBuild(int buildId, [FromServices] IRepository<SoftwareBuild> buildRepo)
        {
            var build = await buildRepo.GetAsync(b => b.Id == buildId);
            if (build == null)
                return NotFound(ApiResponse<string>.FailureResponse("Build not found"));

            buildRepo.Remove(build);
            await buildRepo.SaveAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Build deleted successfully", "Build deleted successfully"));
        }

        [HttpDelete("clients/{clientId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> DeleteClient(int clientId, [FromServices] IRepository<Client> clientRepo)
        {
            var client = await clientRepo.GetAsync(c => c.Id == clientId);
            if (client == null)
                return NotFound(ApiResponse<string>.FailureResponse("Client not found"));

            clientRepo.Remove(client);
            await clientRepo.SaveAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Client deleted successfully", "Client deleted successfully"));
        }

        [HttpDelete("products/{productId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> DeleteProduct(int productId, [FromServices] IRepository<Product> productRepo)
        {
            var product = await productRepo.GetAsync(p => p.Id == productId);
            if (product == null)
                return NotFound(ApiResponse<string>.FailureResponse("Product not found"));

            productRepo.Remove(product);
            await productRepo.SaveAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Product deleted successfully", "Product deleted successfully"));
        }

        [HttpDelete("modules/{moduleId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> DeleteModule(int moduleId, [FromServices] IRepository<Module> moduleRepo)
        {
            var module = await moduleRepo.GetAsync(m => m.Id == moduleId);
            if (module == null)
                return NotFound(ApiResponse<string>.FailureResponse("Module not found"));

            moduleRepo.Remove(module);
            await moduleRepo.SaveAsync();

            return Ok(ApiResponse<string>.SuccessResponse("Module deleted successfully", "Module deleted successfully"));
        }

        [HttpPut("clients/{clientId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> UpdateClient(int clientId, [FromBody] UpdateClientRequestDto request, [FromServices] IRepository<Client> clientRepo)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse<string>.FailureResponse("Client name is required"));

            var client = await clientRepo.GetAsync(c => c.Id == clientId);
            if (client == null)
                return NotFound(ApiResponse<string>.FailureResponse("Client not found"));

            var exists = await clientRepo.Query().AnyAsync(c => c.Id != clientId && c.Name.ToLower() == request.Name.Trim().ToLower());
            if (exists)
                return BadRequest(ApiResponse<string>.FailureResponse("Client name already exists"));

            client.Name = request.Name.Trim();
            client.Description = request.Description?.Trim();
            client.UpdatedAt = DateTime.UtcNow;

            await clientRepo.SaveAsync();

            var dto = new ClientDto
            {
                Id = client.Id,
                ClientNumber = client.ClientNumber,
                Name = client.Name,
                Description = client.Description,
                CreatedAt = client.CreatedAt
            };

            return Ok(ApiResponse<ClientDto>.SuccessResponse(dto, "Client updated successfully"));
        }

        [HttpPut("products/{productId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> UpdateProduct(int productId, [FromBody] UpdateProductRequestDto request, [FromServices] IRepository<Product> productRepo, [FromServices] IRepository<Project> projectRepo)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse<string>.FailureResponse("Product name is required"));

            var product = await productRepo.GetAsync(p => p.Id == productId);
            if (product == null)
                return NotFound(ApiResponse<string>.FailureResponse("Product not found"));

            var projectExists = await projectRepo.Query().AnyAsync(p => p.Id == request.ProjectId);
            if (!projectExists)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found"));

            var exists = await productRepo.Query().AnyAsync(p => p.Id != productId && p.ProjectId == request.ProjectId && p.Name.ToLower() == request.Name.Trim().ToLower());
            if (exists)
                return BadRequest(ApiResponse<string>.FailureResponse("Product name already exists for this project"));

            product.Name = request.Name.Trim();
            product.Description = request.Description?.Trim();
            product.ProjectId = request.ProjectId;
            product.UpdatedAt = DateTime.UtcNow;

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

            return Ok(ApiResponse<ProductDto>.SuccessResponse(dto, "Product updated successfully"));
        }

        [HttpPut("modules/{moduleId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> UpdateModule(int moduleId, [FromBody] UpdateModuleRequestDto request, [FromServices] IRepository<Module> moduleRepo, [FromServices] IRepository<Product> productRepo)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse<string>.FailureResponse("Module name is required"));

            var module = await moduleRepo.GetAsync(m => m.Id == moduleId);
            if (module == null)
                return NotFound(ApiResponse<string>.FailureResponse("Module not found"));

            var productExists = await productRepo.Query().AnyAsync(p => p.Id == request.ProductId);
            if (!productExists)
                return NotFound(ApiResponse<string>.FailureResponse("Product not found"));

            var exists = await moduleRepo.Query().AnyAsync(m => m.Id != moduleId && m.ProductId == request.ProductId && m.Name.ToLower() == request.Name.Trim().ToLower());
            if (exists)
                return BadRequest(ApiResponse<string>.FailureResponse("Module name already exists for this product"));

            module.Name = request.Name.Trim();
            module.Description = request.Description?.Trim();
            module.ProductId = request.ProductId;
            module.UpdatedAt = DateTime.UtcNow;

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

            return Ok(ApiResponse<ModuleDto>.SuccessResponse(dto, "Module updated successfully"));
        }

        [HttpPut("builds/{buildId}")]
        [Authorize(Roles = "ProductManager")]
        public async Task<IActionResult> UpdateBuild(int buildId, [FromBody] UpdateSoftwareBuildRequestDto request, [FromServices] IRepository<SoftwareBuild> buildRepo, [FromServices] IRepository<Project> projectRepo)
        {
            if (string.IsNullOrWhiteSpace(request.BuildNumber))
                return BadRequest(ApiResponse<string>.FailureResponse("Build number is required"));

            var build = await buildRepo.GetAsync(b => b.Id == buildId);
            if (build == null)
                return NotFound(ApiResponse<string>.FailureResponse("Build not found"));

            var projectExists = await projectRepo.Query().AnyAsync(p => p.Id == request.ProjectId);
            if (!projectExists)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found"));

            var exists = await buildRepo.Query().AnyAsync(b => b.Id != buildId && b.ProjectId == request.ProjectId && b.BuildNumber.ToLower() == request.BuildNumber.Trim().ToLower());
            if (exists)
                return BadRequest(ApiResponse<string>.FailureResponse("Build number already exists for this project"));

            build.BuildNumber = request.BuildNumber.Trim();
            build.ProjectId = request.ProjectId;
            build.IsActive = request.IsActive;

            await buildRepo.SaveAsync();

            var created = await buildRepo.Query()
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.Id == build.Id);

            var dto = new SoftwareBuildDto
            {
                Id = created!.Id,
                BuildNumber = created.BuildNumber,
                ProjectId = created.ProjectId,
                ProjectName = created.Project?.Name ?? string.Empty,
                IsActive = created.IsActive,
                CreatedAt = created.CreatedAt
            };

            return Ok(ApiResponse<SoftwareBuildDto>.SuccessResponse(dto, "Build updated successfully"));
        }
    }

    public class PersonalNoteDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime NoteDate { get; set; }
        public string Priority { get; set; } = "medium";
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public int CreatorUserId { get; set; }
        public string? CreatorUserName { get; set; }
    }

    public class CreatePersonalNoteRequest
    {
        public string Content { get; set; } = string.Empty;
        public DateTime? NoteDate { get; set; }
        public string? Priority { get; set; }
        public int? AssignedToUserId { get; set; }
    }
}