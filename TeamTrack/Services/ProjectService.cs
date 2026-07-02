using Microsoft.EntityFrameworkCore;
using TeamTrack.DTOs;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IRepository<Project> _projectRepo;
        private readonly IRepository<WorkItem> _workItemRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Bug> _bugRepo;

        public ProjectService(IRepository<Project> projectRepo, IRepository<WorkItem> workItemRepo, IRepository<User> userRepo, IRepository<Bug> bugRepo)
        {
            _projectRepo = projectRepo;
            _workItemRepo = workItemRepo;
            _userRepo = userRepo;
            _bugRepo = bugRepo;
        }

        public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectRequestDto request, int userId)
        {
            var count = await _projectRepo.Query().CountAsync();
            var project = new Project
            {
                ProjectNumber = $"PRJ-{(count + 1):D3}",
                Name = request.Name,
                Description = request.Description,
                Status = "active",
                CreatedByUserId = userId,
                ClientId = request.ClientId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (request.AssignedEmployeeIds != null && request.AssignedEmployeeIds.Any())
            {
                var employees = await _userRepo.Query()
                    .Where(u => request.AssignedEmployeeIds.Contains(u.Id))
                    .ToListAsync();
                project.AssignedEmployees = employees;
            }

            await _projectRepo.AddAsync(project);
            await _projectRepo.SaveAsync();

            var created = await _projectRepo.Query()
                .Include(p => p.CreatedBy)
                .Include(p => p.AssignedEmployees)
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == project.Id);

            return MapProjectToDto(created!);
        }

        public async Task<List<ProjectResponseDto>> GetAllProjectsAsync(int userId, string userRole)
        {
            var query = _projectRepo.Query()
                .Include(p => p.CreatedBy)
                .Include(p => p.AssignedEmployees)
                .Include(p => p.Client)
                .Include(p => p.WorkItems)
                    .ThenInclude(w => w.AssignedTo);

            List<Project> projects;
            if (userRole == "ProductManager")
            {
                projects = await query.OrderBy(p => p.Id).ToListAsync();
            }
            else
            {
                projects = await query
                    .Where(p => p.AssignedEmployees.Any(e => e.Id == userId))
                    .OrderBy(p => p.Id)
                    .ToListAsync();
            }

            return projects.Select(MapProjectToDto).ToList();
        }

        public async Task<PagedResult<ProjectResponseDto>> GetAllProjectsPagedAsync(int userId, string userRole, int page, int pageSize, string? search)
        {
            var query = _projectRepo.Query()
                .Include(p => p.CreatedBy)
                .Include(p => p.AssignedEmployees)
                .Include(p => p.Client)
                .Include(p => p.WorkItems)
                    .ThenInclude(w => w.AssignedTo)
                .AsQueryable();

            if (userRole != "ProductManager")
                query = query.Where(p => p.AssignedEmployees.Any(e => e.Id == userId));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search) || p.ProjectNumber.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProjectResponseDto>
            {
                Items     = items.Select(MapProjectToDto).ToList(),
                TotalCount = totalCount,
                Page      = page,
                PageSize  = pageSize
            };
        }

        public async Task<ProjectResponseDto?> GetProjectByIdAsync(int projectId)
        {
            var project = await _projectRepo.Query()
                .Include(p => p.CreatedBy)
                .Include(p => p.AssignedEmployees)
                .Include(p => p.Client)
                .Include(p => p.WorkItems)
                    .ThenInclude(w => w.AssignedTo)
                .Include(p => p.WorkItems)
                    .ThenInclude(w => w.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return null;
            return MapProjectToDto(project);
        }

        public async Task<WorkItemResponseDto> CreateWorkItemAsync(int projectId, CreateWorkItemRequestDto request, int userId)
        {
            var count = await _workItemRepo.Query().CountAsync();
            var workItem = new WorkItem
            {
                WorkNumber = $"WRK-{(count + 1):D3}",
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = string.IsNullOrEmpty(request.Status) ? "pending" : request.Status,
                WorkType = string.IsNullOrEmpty(request.WorkType) ? "Task" : request.WorkType,
                StartDate = request.StartDate.HasValue ? DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc) : null,
                ParentId = request.ParentId,
                Labels = request.Labels,
                Team = request.Team,
                AttachmentUrls = request.AttachmentUrls,
                ProjectId = projectId,
                ModuleId = request.ModuleId,
                CreatedByUserId = userId,
                AssignedToUserId = request.AssignedToUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = request.DueDate.HasValue ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc) : null
            };

            await _workItemRepo.AddAsync(workItem);
            await _workItemRepo.SaveAsync();

            // If work type is Bug, automatically create a linked Bug record so it shows up in the bug queues
            if (workItem.WorkType.Equals("Bug", StringComparison.OrdinalIgnoreCase))
            {
                var bugCount = await _bugRepo.Query().CountAsync();
                var bug = new Bug
                {
                    BugNumber = $"BUG-{(bugCount + 1):D3}",
                    Title = workItem.Title,
                    Description = workItem.Description,
                    Status = MapWorkItemStatusToBugStatus(workItem.Status),
                    WorkItemId = workItem.Id,
                    RaisedByUserId = userId,
                    AssignedToUserId = workItem.AssignedToUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _bugRepo.AddAsync(bug);
                await _bugRepo.SaveAsync();
            }

            var created = await _workItemRepo.Query()
                .Include(w => w.Project)
                    .ThenInclude(p => p.Client)
                .Include(w => w.Module)
                    .ThenInclude(m => m.Product)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .FirstOrDefaultAsync(w => w.Id == workItem.Id);

            return MapWorkItemToDto(created!);
        }

        public async Task<List<WorkItemResponseDto>> GetWorkItemsByProjectAsync(int projectId)
        {
            var items = await _workItemRepo.Query()
                .Include(w => w.Project)
                    .ThenInclude(p => p.Client)
                .Include(w => w.Module)
                    .ThenInclude(m => m.Product)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .Where(w => w.ProjectId == projectId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var parentIds = items.Where(w => w.ParentId.HasValue).Select(w => w.ParentId!.Value).Distinct().ToList();
            var parents = await _workItemRepo.Query().Where(p => parentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            return items.Select(w => MapWorkItemToDto(w, parents)).ToList();
        }

        public async Task<PagedResult<WorkItemResponseDto>> GetWorkItemsByProjectPagedAsync(int projectId, int page, int pageSize, string? status, string? search)
        {
            var query = _workItemRepo.Query()
                .Include(w => w.Project)
                    .ThenInclude(p => p.Client)
                .Include(w => w.Module)
                    .ThenInclude(m => m.Product)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .Where(w => w.ProjectId == projectId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
                query = query.Where(w => w.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(w => w.Title.Contains(search) || w.WorkNumber.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var parentIds = items.Where(w => w.ParentId.HasValue).Select(w => w.ParentId!.Value).Distinct().ToList();
            var parents = await _workItemRepo.Query().Where(p => parentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            return new PagedResult<WorkItemResponseDto>
            {
                Items      = items.Select(w => MapWorkItemToDto(w, parents)).ToList(),
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }

        public async Task<List<WorkItemResponseDto>> GetWorkItemsByEmployeeAsync(int userId)
        {
            var items = await _workItemRepo.Query()
                .Include(w => w.Project)
                    .ThenInclude(p => p.Client)
                .Include(w => w.Module)
                    .ThenInclude(m => m.Product)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .Where(w => w.AssignedToUserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var parentIds = items.Where(w => w.ParentId.HasValue).Select(w => w.ParentId!.Value).Distinct().ToList();
            var parents = await _workItemRepo.Query().Where(p => parentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            return items.Select(w => MapWorkItemToDto(w, parents)).ToList();
        }

        public async Task<PagedResult<WorkItemResponseDto>> GetWorkItemsByEmployeePagedAsync(int userId, int page, int pageSize, string? status, string? dueDate, string? search)
        {
            var query = _workItemRepo.Query()
                .Include(w => w.Project)
                    .ThenInclude(p => p.Client)
                .Include(w => w.Module)
                    .ThenInclude(m => m.Product)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .Where(w => w.AssignedToUserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "all")
                query = query.Where(w => w.Status == status);

            if (!string.IsNullOrWhiteSpace(dueDate) && DateOnly.TryParse(dueDate, out var parsedDate))
                query = query.Where(w => w.DueDate.HasValue && DateOnly.FromDateTime(w.DueDate.Value) == parsedDate);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(w => w.Title.Contains(search) || w.WorkNumber.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var parentIds = items.Where(w => w.ParentId.HasValue).Select(w => w.ParentId!.Value).Distinct().ToList();
            var parents = await _workItemRepo.Query().Where(p => parentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            return new PagedResult<WorkItemResponseDto>
            {
                Items      = items.Select(w => MapWorkItemToDto(w, parents)).ToList(),
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }

        public async Task<List<EmployeeDropdownDto>> GetAllEmployeesAsync()
        {
            var employees = await _userRepo.Query()
                .Where(u => u.UserType == "Employee" && u.IsActive)
                .OrderBy(u => u.Name)
                .ToListAsync();

            return employees.Select(e => new EmployeeDropdownDto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email
            }).ToList();
        }

        private ProjectResponseDto MapProjectToDto(Project p)
        {
            var parentIds = p.WorkItems?.Where(w => w.ParentId.HasValue).Select(w => w.ParentId!.Value).Distinct().ToList() ?? [];
            var parentDict = _workItemRepo.Query().Where(parent => parentIds.Contains(parent.Id)).ToDictionary(parent => parent.Id);

            return new ProjectResponseDto
            {
                Id = p.Id,
                ProjectNumber = p.ProjectNumber,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                CreatedBy = p.CreatedBy?.Name ?? string.Empty,
                ClientId = p.ClientId,
                ClientName = p.Client?.Name,
                CreatedAt = p.CreatedAt,
                WorkItems = p.WorkItems?.Select(w => MapWorkItemToDto(w, parentDict)).ToList() ?? [],
                AssignedEmployees = p.AssignedEmployees?.Select(e => new EmployeeDropdownDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email
                }).ToList() ?? []
            };
        }

        private WorkItemResponseDto MapWorkItemToDto(WorkItem w, Dictionary<int, WorkItem>? parentDict = null)
        {
            var dto = new WorkItemResponseDto
            {
                Id = w.Id,
                WorkNumber = w.WorkNumber,
                Title = w.Title,
                Description = w.Description,
                Status = w.Status,
                Priority = w.Priority,
                WorkType = w.WorkType,
                StartDate = w.StartDate,
                ParentId = w.ParentId,
                Labels = w.Labels,
                Team = w.Team,
                AttachmentUrls = w.AttachmentUrls,
                ProjectName = w.Project?.Name ?? string.Empty,
                ProjectNumber = w.Project?.ProjectNumber ?? string.Empty,
                AssignedTo = w.AssignedTo?.Name,
                CreatedBy = w.CreatedBy?.Name ?? string.Empty,
                ModuleId = w.ModuleId,
                ModuleName = w.Module?.Name,
                ProductId = w.Module?.ProductId,
                ProductName = w.Module?.Product?.Name,
                ClientId = w.Project?.ClientId,
                ClientName = w.Project?.Client?.Name,
                CreatedAt = w.CreatedAt,
                DueDate = w.DueDate
            };

            if (w.ParentId.HasValue)
            {
                if (parentDict != null && parentDict.TryGetValue(w.ParentId.Value, out var parent))
                {
                    dto.ParentWorkNumber = parent.WorkNumber;
                    dto.ParentTitle = parent.Title;
                }
                else
                {
                    var parentItem = _workItemRepo.Query().FirstOrDefault(parent => parent.Id == w.ParentId.Value);
                    if (parentItem != null)
                    {
                        dto.ParentWorkNumber = parentItem.WorkNumber;
                        dto.ParentTitle = parentItem.Title;
                    }
                }
            }

            return dto;
        }

        private string MapWorkItemStatusToBugStatus(string wStatus)
        {
            return wStatus.ToLower() switch
            {
                "completed" or "resolved" => "closed",
                "fixed" => "fixed",
                "in_progress" or "waiting_customer" => "in_progress",
                _ => "open"
            };
        }

        public async Task<WorkItemResponseDto?> UpdateWorkItemStatusAsync(int workItemId, UpdateWorkItemStatusRequestDto request)
        {
            var workItem = await _workItemRepo.GetAsync(w => w.Id == workItemId);
            if (workItem == null) return null;

            workItem.Status = request.Status;
            workItem.UpdatedAt = DateTime.UtcNow;

            if (request.Status == "completed" || request.Status == "fixed")
                workItem.CompletedAt = DateTime.UtcNow;

            await _workItemRepo.SaveAsync();

            // If it is a Bug, sync status with the corresponding Bug record
            if (workItem.WorkType.Equals("Bug", StringComparison.OrdinalIgnoreCase))
            {
                var bug = await _bugRepo.GetAsync(b => b.WorkItemId == workItemId);
                if (bug != null)
                {
                    bug.Status = MapWorkItemStatusToBugStatus(request.Status);
                    bug.UpdatedAt = DateTime.UtcNow;
                    if (bug.Status == "fixed") bug.FixedAt = DateTime.UtcNow;
                    if (bug.Status == "closed") bug.ClosedAt = DateTime.UtcNow;
                    await _bugRepo.SaveAsync();
                }
            }

            var updated = await _workItemRepo.Query()
                .Include(w => w.Project)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .FirstOrDefaultAsync(w => w.Id == workItemId);

            return MapWorkItemToDto(updated!);
        }

        public async Task<WorkItemResponseDto?> ReassignWorkItemAsync(int workItemId, ReassignWorkItemRequestDto request)
        {
            var workItem = await _workItemRepo.GetAsync(w => w.Id == workItemId);
            if (workItem == null) return null;

            workItem.AssignedToUserId = request.AssignedToUserId;
            workItem.UpdatedAt = DateTime.UtcNow;

            await _workItemRepo.SaveAsync();

            // If it is a Bug, sync assignee with the corresponding Bug record
            if (workItem.WorkType.Equals("Bug", StringComparison.OrdinalIgnoreCase))
            {
                var bug = await _bugRepo.GetAsync(b => b.WorkItemId == workItemId);
                if (bug != null)
                {
                    bug.AssignedToUserId = request.AssignedToUserId;
                    bug.UpdatedAt = DateTime.UtcNow;
                    await _bugRepo.SaveAsync();
                }
            }

            var updated = await _workItemRepo.Query()
                .Include(w => w.Project)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .FirstOrDefaultAsync(w => w.Id == workItemId);

            return MapWorkItemToDto(updated!);
        }

        public async Task<WorkItemResponseDto?> GetWorkItemByIdAsync(int workItemId)
        {
            var workItem = await _workItemRepo.Query()
                .Include(w => w.Project)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .FirstOrDefaultAsync(w => w.Id == workItemId);

            if (workItem == null) return null;
            return MapWorkItemToDto(workItem);
        }

        public async Task<WorkItemResponseDto?> UpdateWorkItemDueDateAsync(int workItemId, UpdateWorkItemDueDateRequestDto request)
        {
            var workItem = await _workItemRepo.GetAsync(w => w.Id == workItemId);
            if (workItem == null) return null;

            workItem.DueDate = request.DueDate.HasValue ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc) : null;
            workItem.UpdatedAt = DateTime.UtcNow;

            await _workItemRepo.SaveAsync();

            var updated = await _workItemRepo.Query()
                .Include(w => w.Project)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .FirstOrDefaultAsync(w => w.Id == workItemId);

            return MapWorkItemToDto(updated!);
        }

        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            var project = await _projectRepo.GetAsync(p => p.Id == projectId);
            if (project == null) return false;

            _projectRepo.Remove(project);
            await _projectRepo.SaveAsync();
            return true;
        }

        public async Task<List<EmployeeFullDto>> GetAllEmployeesFullAsync()
        {
            var employees = await _userRepo.Query()
                .Where(u => u.UserType == "Employee")
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return employees.Select(e => new EmployeeFullDto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Mobile = e.Mobile,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt
            }).ToList();
        }

        public async Task<ProjectResponseDto?> UpdateProjectMembersAsync(int projectId, UpdateProjectMembersRequestDto request)
        {
            var project = await _projectRepo.Query()
                .Include(p => p.AssignedEmployees)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return null;

            project.AssignedEmployees.Clear();

            if (request.AssignedEmployeeIds != null && request.AssignedEmployeeIds.Any())
            {
                var employees = await _userRepo.Query()
                    .Where(u => request.AssignedEmployeeIds.Contains(u.Id))
                    .ToListAsync();
                project.AssignedEmployees = employees;
            }

            project.UpdatedAt = DateTime.UtcNow;
            await _projectRepo.SaveAsync();

            return MapProjectToDto(project);
        }
    }
}