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
        private readonly IRepository<WorkItemActivityLog> _activityRepo;
        private readonly IRepository<Product> _productRepo;

        public ProjectService(IRepository<Project> projectRepo, IRepository<WorkItem> workItemRepo, IRepository<User> userRepo, IRepository<Bug> bugRepo, IRepository<WorkItemActivityLog> activityRepo, IRepository<Product> productRepo)
        {
            _projectRepo = projectRepo;
            _workItemRepo = workItemRepo;
            _userRepo = userRepo;
            _bugRepo = bugRepo;
            _activityRepo = activityRepo;
            _productRepo = productRepo;
        }

        public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectRequestDto request, int userId)
        {
            var nameExists = await _projectRepo.Query()
                .AnyAsync(p => p.Name.ToLower() == request.Name.Trim().ToLower());
            if (nameExists)
            {
                throw new Exception("A project with this name already exists.");
            }

            // Generate short prefix from project name (e.g. "Sigichai" → "SIG")
            var nameLetters = new string(request.Name.Where(char.IsLetter).ToArray());
            var rawPrefix = nameLetters.Length >= 3 ? nameLetters.Substring(0, 3).ToUpper() : nameLetters.ToUpper().PadRight(3, 'X');

            // Ensure uniqueness: if prefix already used, append a number
            var existingWithPrefix = await _projectRepo.Query()
                .CountAsync(p => p.ProjectNumber.StartsWith(rawPrefix));
            var projectNumber = existingWithPrefix == 0 ? rawPrefix : $"{rawPrefix}{existingWithPrefix + 1}";

            var project = new Project
            {
                ProjectNumber = projectNumber,
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

            var product = new Product
            {
                ProductNumber = $"PRD-{project.ProjectNumber}",
                Name = project.Name,
                Description = project.Description ?? "Auto-generated product for project",
                ProjectId = project.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _productRepo.AddAsync(product);
            await _productRepo.SaveAsync();

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
            {
                var searchLower = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.ProjectNumber.ToLower().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProjectResponseDto>
            {
                Items = items.Select(MapProjectToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
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
            // ── Project-based work number (SIG-001, EMD-002, …) ──────────────
            var project = await _projectRepo.Query().FirstOrDefaultAsync(p => p.Id == projectId);
            var prefix = project?.ProjectNumber ?? "WRK";
            
            string workNumber;
            var isBug = request.WorkType != null && request.WorkType.ToLower() == "bug";
            if (isBug)
            {
                var bugCount = await _workItemRepo.Query().CountAsync(w => w.ProjectId == projectId && w.WorkType.ToLower() == "bug");
                workNumber = $"{prefix}-BUG-{(bugCount + 1):D3}";
            }
            else
            {
                var taskCount = await _workItemRepo.Query().CountAsync(w => w.ProjectId == projectId && w.WorkType.ToLower() != "bug");
                workNumber = $"{prefix}-{(taskCount + 1):D3}";
            }
            // ─────────────────────────────────────────────────────────────────

            var workItem = new WorkItem
            {
                WorkNumber = workNumber,
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
                EpicName = request.EpicName,
                EpicColor = request.EpicColor,
                ProjectId = projectId,
                ModuleId = request.ModuleId,
                CreatedByUserId = userId,
                AssignedToUserId = request.AssignedToUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = request.DueDate.HasValue ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc) : null,
                FixedBillNumber = request.FixedBillNumber,
                RaisedBillNumber = request.RaisedBillNumber,
                DeveloperBillLock = request.DeveloperBillLock,
                RaisedBuild = request.RaisedBuild,
                FixedBuild = request.FixedBuild
            };

            await _workItemRepo.AddAsync(workItem);
            await _workItemRepo.SaveAsync();

            // ── Activity log: Created ──────────────────────────────────────────
            var createLog = new WorkItemActivityLog
            {
                WorkItemId = workItem.Id,
                Action = "Created",
                ByUserId = userId,
                ToUserId = request.AssignedToUserId,
                Note = request.AssignedToUserId.HasValue ? null : "No assignee at creation",
                Timestamp = DateTime.UtcNow
            };
            await _activityRepo.AddAsync(createLog);
            await _activityRepo.SaveAsync();
            // ─────────────────────────────────────────────────────────────────

            // If work type is Bug, automatically create a linked Bug record so it shows up in the bug queues
            if (workItem.WorkType.Equals("Bug", StringComparison.OrdinalIgnoreCase))
            {
                var bug = new Bug
                {
                    BugNumber = workItem.WorkNumber,
                    Title = workItem.Title,
                    Description = workItem.Description,
                    Status = MapWorkItemStatusToBugStatus(workItem.Status),
                    WorkItemId = workItem.Id,
                    RaisedByUserId = userId,
                    AssignedToUserId = workItem.AssignedToUserId,
                    Severity = request.Severity,
                    IssueType = request.IssueType ?? "New",
                    RaisedBuild = request.RaisedBuild,
                    FixedBuild = request.FixedBuild,
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
                .OrderBy(w => w.CreatedAt)
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
            {
                var statuses = status.Split(',').Select(s => s.Trim()).ToList();
                query = query.Where(w => statuses.Contains(w.Status));
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(w => w.Title.Contains(search) || w.WorkNumber.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(w => w.CreatedAt)
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
                 .OrderBy(w => w.CreatedAt)   // Ascending
                .ToListAsync();

            var parentIds = items.Where(w => w.ParentId.HasValue).Select(w => w.ParentId!.Value).Distinct().ToList();
            var parents = await _workItemRepo.Query().Where(p => parentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            return items.Select(w => MapWorkItemToDto(w, parents)).ToList();
        }

        public async Task<PagedResult<WorkItemResponseDto>> GetWorkItemsByEmployeePagedAsync(int userId, int page, int pageSize, string? status, string? dueDate, string? search, string? workType = null, string? priority = null)
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
            {
                var statuses = status.Split(',').Select(s => s.Trim()).ToList();
                query = query.Where(w => statuses.Contains(w.Status));
            }

            if (!string.IsNullOrWhiteSpace(workType) && workType != "all")
                query = query.Where(w => w.WorkType.ToLower() == workType.ToLower());

            if (!string.IsNullOrWhiteSpace(priority) && priority != "all")
                query = query.Where(w => w.Priority.ToLower() == priority.ToLower());

            if (!string.IsNullOrWhiteSpace(dueDate) && DateOnly.TryParse(dueDate, out var parsedDate))
            {
                var startDate = parsedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var endDate = parsedDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                query = query.Where(w => w.DueDate.HasValue && w.DueDate.Value >= startDate && w.DueDate.Value <= endDate);
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(w => w.Title.Contains(search) || w.WorkNumber.Contains(search));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(w => w.CreatedAt)
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
                .Where(u => (u.UserType == "Employee" || u.UserType == "Both") && u.IsActive)
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
                EpicName = w.EpicName,
                EpicColor = w.EpicColor,
                ProjectId = w.ProjectId,
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
                DueDate = w.DueDate,
                AssignedToUserId = w.AssignedToUserId,
                FixedBillNumber = w.FixedBillNumber,
                RaisedBillNumber = w.RaisedBillNumber,
                DeveloperBillLock = w.DeveloperBillLock,
                CreatedByUserId = w.CreatedByUserId,
                RaisedBuild = w.RaisedBuild,
                FixedBuild = w.FixedBuild
            };

            if (w.ParentId.HasValue)
            {
                if (parentDict != null && parentDict.TryGetValue(w.ParentId.Value, out var parent))
                {
                    dto.ParentWorkNumber = parent.WorkNumber;
                    dto.ParentTitle = parent.Title;
                    dto.ParentEpicName = parent.EpicName;
                    dto.ParentEpicColor = parent.EpicColor;
                }
                else
                {
                    var parentItem = _workItemRepo.Query().FirstOrDefault(parent => parent.Id == w.ParentId.Value);
                    if (parentItem != null)
                    {
                        dto.ParentWorkNumber = parentItem.WorkNumber;
                        dto.ParentTitle = parentItem.Title;
                        dto.ParentEpicName = parentItem.EpicName;
                        dto.ParentEpicColor = parentItem.EpicColor;
                    }
                }
            }

            var linkedBug = _bugRepo.Query().FirstOrDefault(b => b.WorkItemId == w.Id);
            if (linkedBug != null)
            {
                dto.Severity = linkedBug.Severity;
                dto.IssueType = linkedBug.IssueType;
                // Only override build info from Bug if Bug has values (don't wipe WorkItem's own build data)
                if (!string.IsNullOrEmpty(linkedBug.RaisedBuild))
                    dto.RaisedBuild = linkedBug.RaisedBuild;
                if (!string.IsNullOrEmpty(linkedBug.FixedBuild))
                    dto.FixedBuild = linkedBug.FixedBuild;
            }

            return dto;
        }

        private string MapWorkItemStatusToBugStatus(string wStatus)
        {
            return wStatus.ToLower() switch
            {
                "completed" or "resolved" => "closed",
                "fixed" => "fixed",
                "in_progress" => "in_progress",
                _ => "open"
            };
        }

        public async Task<WorkItemResponseDto?> UpdateWorkItemStatusAsync(int workItemId, UpdateWorkItemStatusRequestDto request, int byUserId = 0)
        {
            var workItem = await _workItemRepo.GetAsync(w => w.Id == workItemId);
            if (workItem == null) return null;

            var userObj = byUserId > 0 ? await _userRepo.GetAsync(u => u.Id == byUserId) : null;
            var isPM = userObj?.UserType == "ProductManager" || userObj?.UserType == "Both";

            // Enforce developer lock lock-out
            if (workItem.DeveloperBillLock && !isPM)
            {
                throw new Exception("This work item is locked for billing and cannot be edited further.");
            }

            // Process developer checkbox lock selection
            if (request.DeveloperBillLock.HasValue)
            {
                workItem.DeveloperBillLock = request.DeveloperBillLock.Value;
                if (workItem.DeveloperBillLock)
                {
                    workItem.Status = "completed";
                    workItem.CompletedAt = DateTime.UtcNow;
                }
            }

            // Enforce manager-only billing field updates
            if (request.RaisedBillNumber != null || request.FixedBillNumber != null)
            {
                if (!isPM)
                {
                    throw new Exception("Only managers can set or update billing reference numbers.");
                }
                if (request.RaisedBillNumber != null) workItem.RaisedBillNumber = request.RaisedBillNumber;
                if (request.FixedBillNumber != null) workItem.FixedBillNumber = request.FixedBillNumber;
            }

            var oldStatus = workItem.Status;
            workItem.Status = request.Status;
            workItem.UpdatedAt = DateTime.UtcNow;

            // Always save FixedBuild on the WorkItem itself for ALL work types
            if (!string.IsNullOrEmpty(request.FixedBuild))
            {
                workItem.FixedBuild = request.FixedBuild;
            }

            if (request.Status == "completed" || request.Status == "fixed")
                workItem.CompletedAt = DateTime.UtcNow;

            // Also sync FixedBuild to the linked Bug record if this is a Bug type
            if (workItem.WorkType.Equals("Bug", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(request.FixedBuild))
            {
                var bug = await _bugRepo.GetAsync(b => b.WorkItemId == workItem.Id);
                if (bug != null)
                {
                    bug.FixedBuild = request.FixedBuild;
                    bug.UpdatedAt = DateTime.UtcNow;
                    await _bugRepo.SaveAsync();
                }
            }

            await _workItemRepo.SaveAsync();

            // Status synchronization with parent epic
            if (workItem.ParentId.HasValue)
            {
                var parent = await _workItemRepo.GetAsync(p => p.Id == workItem.ParentId.Value);
                if (parent != null && parent.WorkType.Equals("Epic", StringComparison.OrdinalIgnoreCase))
                {
                    // If child transitions to in_progress, transition parent Epic to in_progress if not already active/resolved
                    if (request.Status.Equals("in_progress", StringComparison.OrdinalIgnoreCase) && 
                        !parent.Status.Equals("in_progress", StringComparison.OrdinalIgnoreCase) && 
                        !parent.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) && 
                        !parent.Status.Equals("closed", StringComparison.OrdinalIgnoreCase))
                    {
                        var oldParentStatus = parent.Status;
                        parent.Status = "in_progress";
                        parent.UpdatedAt = DateTime.UtcNow;
                        await _workItemRepo.SaveAsync();
                        await LogActivityAsync(parent.Id, "StatusChanged", oldParentStatus, "in_progress", byUserId);
                    }
                    // If child transitions to resolved/completed, auto-resolve parent Epic if all children are now resolved/completed
                    else if (request.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) || 
                             request.Status.Equals("closed", StringComparison.OrdinalIgnoreCase))
                    {
                        var siblingWorkItems = await _workItemRepo.Query()
                            .Where(w => w.ParentId == parent.Id && w.Id != workItem.Id)
                            .ToListAsync();

                        var allSiblingsCompleted = siblingWorkItems.All(w => 
                            w.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) || 
                            w.Status.Equals("closed", StringComparison.OrdinalIgnoreCase) || 
                            w.Status.Equals("fixed", StringComparison.OrdinalIgnoreCase));

                        if (allSiblingsCompleted)
                        {
                            var oldParentStatus = parent.Status;
                            parent.Status = "completed";
                            parent.CompletedAt = DateTime.UtcNow;
                            parent.UpdatedAt = DateTime.UtcNow;
                            await _workItemRepo.SaveAsync();
                            await LogActivityAsync(parent.Id, "StatusChanged", oldParentStatus, "completed", byUserId);
                        }
                    }
                }
            }

            // ── Activity log: StatusChanged ───────────────────────────────────
            if (byUserId > 0 && oldStatus != request.Status)
            {
                var statusLog = new WorkItemActivityLog
                {
                    WorkItemId = workItemId,
                    Action = "StatusChanged",
                    FromStatus = oldStatus,
                    ToStatus = request.Status,
                    ByUserId = byUserId,
                    Timestamp = DateTime.UtcNow
                };
                await _activityRepo.AddAsync(statusLog);
                await _activityRepo.SaveAsync();
            }
            // ─────────────────────────────────────────────────────────────────

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

        public async Task<WorkItemResponseDto?> ReassignWorkItemAsync(int workItemId, ReassignWorkItemRequestDto request, int byUserId = 0)
        {
            var workItem = await _workItemRepo.GetAsync(w => w.Id == workItemId);
            if (workItem == null) return null;

            var oldAssigneeId = workItem.AssignedToUserId;
            workItem.AssignedToUserId = request.AssignedToUserId;
            workItem.UpdatedAt = DateTime.UtcNow;

            await _workItemRepo.SaveAsync();

            // ── Activity log: Reassigned ──────────────────────────────────────
            var action = oldAssigneeId.HasValue ? "Reassigned" : "Assigned";
            var reassignLog = new WorkItemActivityLog
            {
                WorkItemId = workItemId,
                Action = action,
                FromUserId = oldAssigneeId,
                ToUserId = request.AssignedToUserId,
                ByUserId = byUserId > 0 ? byUserId : request.AssignedToUserId,
                Timestamp = DateTime.UtcNow
            };
            await _activityRepo.AddAsync(reassignLog);
            await _activityRepo.SaveAsync();
            // ─────────────────────────────────────────────────────────────────

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

        public async Task<List<WorkItemActivityLogDto>> GetWorkItemActivityAsync(int workItemId)
        {
            var logs = await _activityRepo.Query()
                .Include(a => a.ByUser)
                .Include(a => a.FromUser)
                .Include(a => a.ToUser)
                .Where(a => a.WorkItemId == workItemId)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();

            return logs.Select(a => new WorkItemActivityLogDto
            {
                Id        = a.Id,
                Action    = a.Action,
                FromUser  = a.FromUser?.Name,
                ToUser    = a.ToUser?.Name,
                FromStatus = a.FromStatus,
                ToStatus  = a.ToStatus,
                ByUser    = a.ByUser?.Name ?? "System",
                Note      = a.Note,
                Timestamp = a.Timestamp
            }).ToList();
        }

        /// <summary>
        /// Returns all work items the user was EVER involved in via the activity log
        /// (created, assigned, reassigned from/to) — even after they were reassigned away.
        /// This lets Employee 1 see history of tasks they previously owned.
        /// </summary>
        public async Task<List<WorkItemResponseDto>> GetInvolvedWorkItemsAsync(int userId)
        {
            // Find all workItemIds where this user appears in the activity log
            var involvedWorkItemIds = await _activityRepo.Query()
                .Where(a =>
                    a.FromUserId == userId ||
                    a.ToUserId   == userId ||
                    a.ByUserId   == userId)
                .Select(a => a.WorkItemId)
                .Distinct()
                .ToListAsync();

            if (!involvedWorkItemIds.Any())
                return [];

            // Fetch the full work items (excluding ones currently assigned to this user — those show in MyTasks)
            var items = await _workItemRepo.Query()
                .Include(w => w.Project).ThenInclude(p => p.Client)
                .Include(w => w.Module).ThenInclude(m => m.Product)
                .Include(w => w.AssignedTo)
                .Include(w => w.CreatedBy)
                .Where(w => involvedWorkItemIds.Contains(w.Id) && w.AssignedToUserId != userId)
                .OrderBy(w => w.UpdatedAt)
                .ToListAsync();

            var parentIds = items.Where(w => w.ParentId.HasValue).Select(w => w.ParentId!.Value).Distinct().ToList();
            var parents = await _workItemRepo.Query().Where(p => parentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            return items.Select(w => MapWorkItemToDto(w, parents)).ToList();
        }

        public async Task<List<EmployeeFullDto>> GetAllEmployeesFullAsync()
        {
            var employees = await _userRepo.Query()
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            return employees.Select(e => new EmployeeFullDto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Mobile = e.Mobile,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UserType = e.UserType
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

        public async Task<ProjectResponseDto?> UpdateProjectAsync(int projectId, UpdateProjectRequestDto request)
        {
            var project = await _projectRepo.Query()
                .Include(p => p.AssignedEmployees)
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return null;

            var nameExists = await _projectRepo.Query()
                .AnyAsync(p => p.Id != projectId && p.Name.ToLower() == request.Name.Trim().ToLower());
            if (nameExists)
            {
                throw new Exception("A project with this name already exists.");
            }

            project.Name = request.Name;
            project.Description = request.Description;
            project.Status = request.Status;
            project.ClientId = request.ClientId;
            project.UpdatedAt = DateTime.UtcNow;

            await _projectRepo.SaveAsync();

            var product = await _productRepo.GetAsync(p => p.ProjectId == projectId);
            if (product != null)
            {
                product.Name = request.Name;
                product.Description = request.Description ?? "Auto-generated product for project";
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepo.SaveAsync();
            }

            return MapProjectToDto(project);
        }

        private async Task LogActivityAsync(int workItemId, string action, string? oldStatus, string? toStatus, int byUserId)
        {
            if (byUserId <= 0) return;
            var log = new WorkItemActivityLog
            {
                WorkItemId = workItemId,
                Action = action,
                FromStatus = oldStatus,
                ToStatus = toStatus,
                ByUserId = byUserId,
                Timestamp = DateTime.UtcNow
            };
            await _activityRepo.AddAsync(log);
            await _activityRepo.SaveAsync();
        }
    }
}